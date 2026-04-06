using System.Reflection;
using System.Text;
using AwesomeAssertions;
using GitCommands;
using GitCommands.Settings;

namespace GitCommandsTests.Settings;

[TestFixture]
internal sealed class AppSettingsTests
{
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
    public void SetDocumentationBaseUrl_should_currectly_append_version(string version, string expected)
    {
        AppSettings.GetTestAccessor().ResetDocumentationBaseUrl();

        AppSettings.SetDocumentationBaseUrl(version);
        AppSettings.DocumentationBaseUrl.Should().Be(expected);
    }

    /// <summary>
    ///  Verifies that ISetting property names (storage keys) remain stable across changes.
    ///  If a property is added, removed, or renamed, this test will fail and the
    ///  <c>.verified.txt</c> snapshot must be reviewed and updated.
    /// </summary>
    [Test]
    public Task ISetting_properties_should_have_stable_storage_keys()
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
            sb.AppendLine($"{property.Name} = {fullPath}");
        }

        return Verifier.Verify(sb.ToString());
    }

    private static bool IsISettingType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ISetting<>))
        {
            return true;
        }

        return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISetting<>));
    }
}
