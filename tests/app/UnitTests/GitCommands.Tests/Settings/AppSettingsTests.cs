using System.CodeDom.Compiler;
using System.Reflection;
using System.Text;
using GitCommands;
using GitCommands.Settings;
using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Settings;
using GitUIPluginInterfaces;
using Microsoft;

namespace GitCommandsTests.Settings;
internal sealed class AppSettingsTests
{
    private const string SettingsFileContent = @"<?xml version=""1.0"" encoding=""utf-8""?><dictionary />";

    [TestCase(null, "https://git-extensions-documentation.readthedocs.org/en/main/")]
    [TestCase("", "https://git-extensions-documentation.readthedocs.org/en/main/")]
    [TestCase("\t", "https://git-extensions-documentation.readthedocs.org/en/main/")]
    [TestCase("33.33", "https://git-extensions-documentation.readthedocs.org/en/main/")]
    [TestCase("33.33.33", "https://git-extensions-documentation.readthedocs.org/en/main/")]
    [TestCase("33.33.33.33", "https://git-extensions-documentation.readthedocs.org/en/main/")]
    [TestCase("a", "https://git-extensions-documentation.readthedocs.org/en/main/")]
    [TestCase("5", "https://git-extensions-documentation.readthedocs.org/en/main/")]
    [TestCase("v4.5", "https://git-extensions-documentation.readthedocs.org/en/main/")]
    [TestCase("4.5", "https://git-extensions-documentation.readthedocs.org/en/release-4.5/")]
    [TestCase("4.5.", "https://git-extensions-documentation.readthedocs.org/en/release-4.5/")]
    [TestCase("4.5.0", "https://git-extensions-documentation.readthedocs.org/en/release-4.5/")]
    [TestCase("4.5.2", "https://git-extensions-documentation.readthedocs.org/en/release-4.5/")]
    [TestCase("4.5.2.1", "https://git-extensions-documentation.readthedocs.org/en/release-4.5/")]
    [TestCase("4.5.2x", "https://git-extensions-documentation.readthedocs.org/en/release-4.5/")]
    [TestCase("40.501.123", "https://git-extensions-documentation.readthedocs.org/en/release-40.501/")]
    public void SetDocumentationBaseUrl_should_currectly_append_version(string? version, string expected)
    {
        AppSettings.GetTestAccessor().ResetDocumentationBaseUrl();

        AppSettings.SetDocumentationBaseUrl(version!);
        AppSettings.DocumentationBaseUrl.Should().Be(expected);
    }

    [Test]
    [TestCaseSource(nameof(TestCases))]
#pragma warning disable IDE0060 // Remove unused parameter
    public void Should_return_default_value(PropertyInfo property, object value, object defaultValue, bool isISetting)
#pragma warning restore IDE0060 // Remove unused parameter
    {
        // Arrange
        object? root = null;

        if (isISetting)
        {
            root = property.GetValue(null);

            property = property.PropertyType
                .GetProperty(nameof(ISetting<>.Value))!;
        }

        using TempFileCollection tempFiles = new();
        string filePath = tempFiles.AddExtension(".settings");
        tempFiles.AddFile(filePath + ".backup", keepFile: false);

        File.WriteAllText(filePath, SettingsFileContent);

        using GitExtSettingsCache cache = GitExtSettingsCache.Create(filePath);
        DistributedSettings container = new(lowerPriority: null, cache, SettingLevel.Unknown);
        object? storedValue = null;

        // Act
        AppSettings.UsingContainer(container, () =>
        {
            storedValue = property!.GetValue(root);
        });

        // Assert
        storedValue.Should().Be(defaultValue);
    }

    [Test]
    [TestCaseSource(nameof(TestCases))]
    public void Should_save_value(PropertyInfo property, object value, object defaultValue, bool isISetting)
    {
        // Arrange
        object? root = null;

        if (isISetting)
        {
            root = property.GetValue(null);

            property = property.PropertyType
                .GetProperty(nameof(ISetting<>.Value))!;
        }

        using TempFileCollection tempFiles = new();
        string filePath = tempFiles.AddExtension(".settings");
        tempFiles.AddFile(filePath + ".backup", keepFile: false);

        File.WriteAllText(filePath, SettingsFileContent);

        using GitExtSettingsCache cache = GitExtSettingsCache.Create(filePath);
        DistributedSettings container = new(lowerPriority: null, cache, SettingLevel.Unknown);
        object? storedValue = null;

        // Act
        AppSettings.UsingContainer(container, () =>
        {
            property!.SetValue(root, value);

            storedValue = property.GetValue(root);
        });

        // Assert
        if (Type.GetTypeCode(property!.PropertyType) == TypeCode.String)
        {
            if (isISetting)
            {
                storedValue.Should().Be(value ?? string.Empty);
            }
            else
            {
                storedValue.Should().Be(value ?? defaultValue);
            }
        }
        else if (Type.GetTypeCode(property.PropertyType) == TypeCode.DateTime)
        {
            // We keep only the date
            storedValue.Should().Be(((DateTime)value).Date);
        }
        else
        {
            // The validated property stores the clamped value
            object expectedValue = property.Name == nameof(AppSettings.MaxDiffFileSizeMb)
                ? Math.Clamp((int)value, 0, 100)
                : value;
            storedValue.Should().Be(expectedValue);
        }
    }

    [Test]
    [TestCase(-5, 0)]
    [TestCase(0, 0)]
    [TestCase(5, 5)]
    [TestCase(100, 100)]
    [TestCase(101, 100)]
    [TestCase(10000, 100)]
    public void MaxDiffFileSizeMb_should_clamp_value_to_valid_range(int input, int expected)
    {
        using TempFileCollection tempFiles = new();
        string filePath = tempFiles.AddExtension(".settings");
        tempFiles.AddFile(filePath + ".backup", keepFile: false);
        File.WriteAllText(filePath, SettingsFileContent);
        using GitExtSettingsCache cache = GitExtSettingsCache.Create(filePath);
        DistributedSettings container = new(lowerPriority: null, cache, SettingLevel.Unknown);

        AppSettings.UsingContainer(container, () =>
        {
            AppSettings.MaxDiffFileSizeMb = input;
            AppSettings.MaxDiffFileSizeMb.Should().Be(expected);
        });
    }

    [Test]
    [TestCase("-5", 0)]
    [TestCase("5000", 100)]
    public void MaxDiffFileSizeMb_should_clamp_invalid_persisted_value(string storedValue, int expected)
    {
        using TempFileCollection tempFiles = new();
        string filePath = tempFiles.AddExtension(".settings");
        tempFiles.AddFile(filePath + ".backup", keepFile: false);
        // The settings file stores values wrapped in a <string> element, matching
        // the format produced by GitExtSettingsCache.WriteSettings.
        File.WriteAllText(filePath, $@"<?xml version=""1.0"" encoding=""utf-8""?><dictionary><item><key><string>maxdifffilesize</string></key><value><string>{storedValue}</string></value></item></dictionary>");
        using GitExtSettingsCache cache = GitExtSettingsCache.Create(filePath);
        DistributedSettings container = new(lowerPriority: null, cache, SettingLevel.Unknown);

        AppSettings.UsingContainer(container, () =>
        {
            AppSettings.MaxDiffFileSizeMb.Should().Be(expected);
        });
    }

    /// <summary>
    ///  Verifies that ISetting property names (storage keys) remain stable across changes.
    /// </summary>
    [Test]
    public Task ISetting_properties_should_have_stable_storage_keys_and_type()
    {
        StringBuilder sb = new();
        IOrderedEnumerable<PropertyInfo> properties = typeof(AppSettings)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => IsISettingType(p.PropertyType))
            .OrderBy(p => p.Name, StringComparer.Ordinal);
        foreach (PropertyInfo property in properties)
        {
            object setting = property.GetValue(null)!;
            string fullPath = (string)setting.GetType()
                .GetProperty(nameof(ISetting<int>.FullPath))!
                .GetValue(setting)!;
            Type type = property.PropertyType;
            sb.Append(type.Name);
            if (type.IsGenericType)
            {
                sb.Append('<').Append(type.GenericTypeArguments[0].Name).Append('>');
            }

            sb.AppendLine($" {property.Name} = {fullPath}");
        }

        return Verifier.Verify(sb.ToString());

        static bool IsISettingType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ISetting<>))
            {
                return true;
            }

            return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISetting<>));
        }
    }

    #region Test Cases

    private static IEnumerable<object[]> TestCases()
    {
        foreach ((PropertyInfo property, object? defaultValue, bool isNullable, bool isISetting) in PropertyInfos())
        {
            if (isNullable)
            {
                yield return new object[] { property, null!, defaultValue!, isISetting };
            }

            Type? propertyType = property.PropertyType;
            if (isISetting)
            {
                propertyType = propertyType.GetProperty(nameof(ISetting<>.Value))?.PropertyType;
            }

            propertyType = Nullable.GetUnderlyingType(propertyType!) ?? propertyType;
            Validates.NotNull(propertyType);

            foreach (object value in Values())
            {
                Type valueType = Nullable.GetUnderlyingType(value.GetType()) ?? value.GetType();
                if (valueType == propertyType)
                {
                    yield return new object[] { property, value, defaultValue!, isISetting };
                }
            }
        }

        static IEnumerable<(PropertyInfo property, object? defaultValue, bool isNullable, bool isISetting)> PropertyInfos()
        {
            Dictionary<string, PropertyInfo> properties = typeof(AppSettings).GetProperties()
                .ToDictionary(x => x.Name, x => x);

            const bool isNoISetting = false;
            const bool isNotNullable = false;
            yield return (properties[nameof(AppSettings.AvatarProvider)], AvatarProvider.None, false, false);
            yield return (properties[nameof(AppSettings.UserProfileHomeDir)], false, false, false);
            yield return (properties[nameof(AppSettings.DefaultPullAction)], GitPullAction.Merge, false, false);
            yield return (properties[nameof(AppSettings.FormPullAction)], GitPullAction.Merge, false, false);
            yield return (properties[nameof(AppSettings.AutoPullOnPushRejectedAction)], null, true, false);
            yield return (properties[nameof(AppSettings.HideMergeCommits)], false, false, false);
            yield return (properties[nameof(AppSettings.MaxDiffFileSizeMb)], 5, isNotNullable, isNoISetting);
            yield return (properties[nameof(AppSettings.RecentWorkingDir)], null, true, false);
            yield return (properties[nameof(AppSettings.OwnScripts)], string.Empty, true, false);
            yield return (properties[nameof(AppSettings.ShorteningRecentRepoPathStrategy)], ShorteningRecentRepoPathStrategy.MostSignDir, false, false);
            yield return (properties[nameof(AppSettings.SerializedHotkeys)], null, true, false);
            yield return (properties[nameof(AppSettings.CommitValidationRegEx)], string.Empty, true, false);
            yield return (properties[nameof(AppSettings.CommitTemplates)], string.Empty, true, false);
            yield return (properties[nameof(AppSettings.RefsSortBy)], GitRefsSortBy.Default, false, false);
            yield return (properties[nameof(AppSettings.RefsSortOrder)], GitRefsSortOrder.Descending, false, false);
            yield return (properties[nameof(AppSettings.DiffListSorting)], DiffListSortType.FilePath, false, false);
        }

        static IEnumerable<object> Values()
        {
            yield return string.Empty;
            yield return " ";
            yield return "0";

            yield return false;
            yield return true;

            yield return char.MinValue;
            yield return char.MaxValue;
            yield return ' ';
            yield return '0';

            yield return byte.MinValue;
            yield return byte.MaxValue;

            yield return int.MinValue;
            yield return int.MaxValue;
            yield return 0;
            yield return 1;
            yield return -1;

            yield return float.MinValue;
            yield return float.MaxValue;
            yield return float.Epsilon;
            yield return float.PositiveInfinity;
            yield return float.NegativeInfinity;
            yield return float.NaN;
            yield return 0f;
            yield return 1f;
            yield return -1f;

            yield return double.MinValue;
            yield return double.MaxValue;
            yield return double.Epsilon;
            yield return double.PositiveInfinity;
            yield return double.NegativeInfinity;
            yield return double.NaN;
            yield return 0d;
            yield return 1d;
            yield return -1d;

            yield return decimal.MinValue;
            yield return decimal.MaxValue;
            yield return decimal.Zero;
            yield return decimal.One;
            yield return decimal.MinusOne;

            yield return DateTime.MinValue;
            yield return DateTime.MaxValue;
            yield return DateTime.Today;

            Type[] enumTypes =
            [
                typeof(TruncatePathMethod),
                typeof(AvatarProvider),
                typeof(GitPullAction),
                typeof(LocalChangesAction),
                typeof(IgnoreWhitespaceKind),
                typeof(ShorteningRecentRepoPathStrategy),
                typeof(GitRefsSortBy),
                typeof(GitRefsSortOrder),
                typeof(DiffListSortType),
                typeof(RevisionSortOrder),
            ];

            foreach (Type enumType in enumTypes)
            {
                foreach (object enumValue in Enum.GetValues(enumType))
                {
                    yield return enumValue;
                }
            }
        }
    }

    #endregion Test Cases
}
