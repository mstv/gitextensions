using System.CodeDom.Compiler;
using AwesomeAssertions;
using GitCommands;
using GitCommands.Settings;
using GitExtensions.Extensibility.Settings;

namespace GitCommandsTests.Settings;

[TestFixture]
internal sealed class InterceptedAppSettingsTests
{
    private const string SettingsFileContent = @"<?xml version=""1.0"" encoding=""utf-8""?><dictionary />";

    private TempFileCollection _tempFiles = null!;
    private GitExtSettingsCache _gitExtSettingsCache = null!;
    private DistributedSettings _settingContainer = null!;

    [SetUp]
    public void SetUp()
    {
        _tempFiles = new TempFileCollection();
        string settingFilePath = _tempFiles.AddExtension(".settings");
        _tempFiles.AddFile(settingFilePath + ".backup", keepFile: false);

        File.WriteAllText(settingFilePath, SettingsFileContent);

        _gitExtSettingsCache = GitExtSettingsCache.Create(settingFilePath);
        _settingContainer = new DistributedSettings(lowerPriority: null, _gitExtSettingsCache, SettingLevel.Unknown);
    }

    [TearDown]
    public void TearDown()
    {
        _gitExtSettingsCache.Dispose();
        ((IDisposable)_tempFiles).Dispose();
    }

    #region AutoNormaliseSymbol

    [Test]
    public void AutoNormaliseSymbol_default_should_be_underscore()
    {
        AppSettings.UsingContainer(_settingContainer, () =>
        {
            AppSettings.AutoNormaliseSymbol.Value.Should().Be("_");
        });
    }

    [Test]
    public void AutoNormaliseSymbol_empty_should_store_plus_and_read_back_empty()
    {
        AppSettings.UsingContainer(_settingContainer, () =>
        {
            AppSettings.AutoNormaliseSymbol.Value = "";

            AppSettings.AutoNormaliseSymbol.Value.Should().Be("");
            AppSettings.SettingsContainer.GetValue("AutoNormaliseSymbol").Should().Be("+");
        });
    }

    [Test]
    public void AutoNormaliseSymbol_dash_should_round_trip()
    {
        AppSettings.UsingContainer(_settingContainer, () =>
        {
            AppSettings.AutoNormaliseSymbol.Value = "-";

            AppSettings.AutoNormaliseSymbol.Value.Should().Be("-");
            AppSettings.SettingsContainer.GetValue("AutoNormaliseSymbol").Should().Be("-");
        });
    }

    [Test]
    public void AutoNormaliseSymbol_whitespace_should_store_plus_and_read_back_empty()
    {
        AppSettings.UsingContainer(_settingContainer, () =>
        {
            AppSettings.AutoNormaliseSymbol.Value = " ";

            AppSettings.AutoNormaliseSymbol.Value.Should().Be("");
            AppSettings.SettingsContainer.GetValue("AutoNormaliseSymbol").Should().Be("+");
        });
    }

    #endregion AutoNormaliseSymbol

    #region DontConfirmStashDrop

    [Test]
    public void DontConfirmStashDrop_default_should_be_false()
    {
        AppSettings.UsingContainer(_settingContainer, () =>
        {
            AppSettings.DontConfirmStashDrop.Value.Should().BeFalse();
        });
    }

    [Test]
    public void DontConfirmStashDrop_setting_true_should_store_false()
    {
        AppSettings.UsingContainer(_settingContainer, () =>
        {
            AppSettings.DontConfirmStashDrop.Value = true;

            AppSettings.DontConfirmStashDrop.Value.Should().BeTrue();
            AppSettings.SettingsContainer.GetValue("stashconfirmdropshow").Should().Be("False");
        });
    }

    [Test]
    public void DontConfirmStashDrop_setting_false_should_store_true()
    {
        AppSettings.UsingContainer(_settingContainer, () =>
        {
            AppSettings.DontConfirmStashDrop.Value = false;

            AppSettings.DontConfirmStashDrop.Value.Should().BeFalse();
            AppSettings.SettingsContainer.GetValue("stashconfirmdropshow").Should().Be("True");
        });
    }

    #endregion DontConfirmStashDrop

    #region HideMergeCommits

    [Test]
    public void HideMergeCommits_default_should_be_false()
    {
        AppSettings.UsingContainer(_settingContainer, () =>
        {
            AppSettings.HideMergeCommits.Value.Should().BeFalse();
        });
    }

    [Test]
    public void HideMergeCommits_setting_true_should_store_false()
    {
        AppSettings.UsingContainer(_settingContainer, () =>
        {
            AppSettings.HideMergeCommits.Value = true;

            AppSettings.HideMergeCommits.Value.Should().BeTrue();
            AppSettings.SettingsContainer.GetValue("showmergecommits").Should().Be("False");
        });
    }

    [Test]
    public void HideMergeCommits_setting_false_should_store_true()
    {
        AppSettings.UsingContainer(_settingContainer, () =>
        {
            AppSettings.HideMergeCommits.Value = false;

            AppSettings.HideMergeCommits.Value.Should().BeFalse();
            AppSettings.SettingsContainer.GetValue("showmergecommits").Should().Be("True");
        });
    }

    #endregion HideMergeCommits

    #region AvatarProvider

    [Test]
    public void AvatarProvider_default_should_be_None()
    {
        AppSettings.UsingContainer(_settingContainer, () =>
        {
            AppSettings.AvatarProvider.Value.Should().Be(GitCommands.AvatarProvider.None);
        });
    }

    [Test]
    public void AvatarProvider_legacy_AuthorInitials_should_read_as_None()
    {
        AppSettings.UsingContainer(_settingContainer, () =>
        {
            AppSettings.SettingsContainer.SetValue("Appearance.AvatarProvider", "AuthorInitials");

            AppSettings.AvatarProvider.Value.Should().Be(GitCommands.AvatarProvider.None);
        });
    }

    [Test]
    public void AvatarProvider_legacy_Gravatar_should_read_as_Default()
    {
        AppSettings.UsingContainer(_settingContainer, () =>
        {
            AppSettings.SettingsContainer.SetValue("Appearance.AvatarProvider", "Gravatar");

            AppSettings.AvatarProvider.Value.Should().Be(GitCommands.AvatarProvider.Default);
        });
    }

    [Test]
    public void AvatarProvider_current_Default_should_read_as_Default()
    {
        AppSettings.UsingContainer(_settingContainer, () =>
        {
            AppSettings.SettingsContainer.SetValue("Appearance.AvatarProvider", "Default");

            AppSettings.AvatarProvider.Value.Should().Be(GitCommands.AvatarProvider.Default);
        });
    }

    [Test]
    public void AvatarProvider_setting_Default_should_store_Default_string()
    {
        AppSettings.UsingContainer(_settingContainer, () =>
        {
            AppSettings.AvatarProvider.Value = GitCommands.AvatarProvider.Default;

            AppSettings.SettingsContainer.GetValue("Appearance.AvatarProvider").Should().Be("Default");
        });
    }

    #endregion AvatarProvider

    #region AvatarFallbackType

    [Test]
    public void AvatarFallbackType_default_should_be_AuthorInitials()
    {
        AppSettings.UsingContainer(_settingContainer, () =>
        {
            AppSettings.AvatarFallbackType.Value.Should().Be(GitCommands.AvatarFallbackType.AuthorInitials);
        });
    }

    [Test]
    public void AvatarFallbackType_legacy_None_should_read_as_AuthorInitials()
    {
        AppSettings.UsingContainer(_settingContainer, () =>
        {
            AppSettings.SettingsContainer.SetValue("GravatarDefaultImageType", "None");

            AppSettings.AvatarFallbackType.Value.Should().Be(GitCommands.AvatarFallbackType.AuthorInitials);
        });
    }

    [Test]
    public void AvatarFallbackType_current_MonsterId_should_read_as_MonsterId()
    {
        AppSettings.UsingContainer(_settingContainer, () =>
        {
            AppSettings.SettingsContainer.SetValue("GravatarDefaultImageType", "MonsterId");

            AppSettings.AvatarFallbackType.Value.Should().Be(GitCommands.AvatarFallbackType.MonsterId);
        });
    }

    #endregion AvatarFallbackType
}
