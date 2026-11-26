using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using GitCommands.Git;
using GitCommands.Settings;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Configurations;
using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Settings;
using GitExtUtils.GitUI.Theming;
using GitUIPluginInterfaces;
using Microsoft;
using Microsoft.Win32;

namespace GitCommands;

public static partial class AppSettings
{
    // semi-constants
    public static Version AppVersion => Assembly.GetCallingAssembly().GetName().Version!;
    public static string ProductVersion => Application.ProductVersion;
    public static readonly string ApplicationName = "Git Extensions";
    public static readonly string ApplicationId = ApplicationName.Replace(" ", "");
    public static readonly string SettingsFileName = ApplicationId + ".settings";
    public static readonly string UserPluginsDirectoryName = "UserPlugins";
    private static string _applicationExecutablePath = Application.ExecutablePath;
    private static string? _documentationBaseUrl;

    public static Lazy<string?> ApplicationDataPath { get; private set; }
    public static readonly Lazy<string?> LocalApplicationDataPath;
    public static string SettingsFilePath => Path.Join(ApplicationDataPath.Value!, SettingsFileName);
    public static string UserPluginsPath => Path.Join(LocalApplicationDataPath.Value!, UserPluginsDirectoryName);

    public static DistributedSettings SettingsContainer { get; private set; }

    private static Mutex? _globalMutex;

    [GeneratedRegex(@"^(?<major>\d+)\.(?<minor>\d+)", RegexOptions.ExplicitCapture)]
    private static partial Regex VersionRegex { get; }

    public static event Action? Saved;

    static AppSettings()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        ApplicationDataPath = new Lazy<string?>(() =>
        {
            if (IsPortable())
            {
                return GetGitExtensionsDirectory();
            }

            // Make ApplicationDataPath version independent
            return Application.UserAppDataPath.Replace(Application.ProductVersion, string.Empty)
                                              .Replace(ApplicationName, ApplicationId); // 'GitExtensions' has been changed to 'Git Extensions' in v3.0
        });

        LocalApplicationDataPath = new Lazy<string?>(() =>
        {
            if (IsPortable())
            {
                return GetGitExtensionsDirectory();
            }

            string path = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ApplicationId);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        });

        bool newFile = CreateEmptySettingsFileIfMissing();

        SettingsContainer = new DistributedSettings(lowerPriority: null, GitExtSettingsCache.FromCache(SettingsFilePath), SettingLevel.Unknown);

        if (newFile || !File.Exists(SettingsFilePath))
        {
            ImportFromRegistry();
        }

        MigrateAvatarSettings();
        MigrateSshSettings();

        return;

        static bool CreateEmptySettingsFileIfMissing()
        {
            try
            {
                string? dir = Path.GetDirectoryName(SettingsFilePath);
                if (!Directory.Exists(dir) || File.Exists(SettingsFilePath))
                {
                    return false;
                }
            }
            catch (ArgumentException)
            {
                // Illegal characters in the filename
                return false;
            }

            File.WriteAllText(SettingsFilePath, "<?xml version=\"1.0\" encoding=\"utf-8\"?><dictionary />", Encoding.UTF8);
            return true;
        }
    }

    /// <summary>
    /// Gets the base part of the documentation link for the current application version,
    /// which looks something like "https://git-extensions-documentation.readthedocs.org/en/main/"
    /// for the master branch, and "https://git-extensions-documentation.readthedocs.org/en/release-X.Y/"
    /// for a release/X.Y branch.
    ///
    /// TODO: We currently use only EN language, but should maybe consider using the user's preferred language.
    /// </summary>
    public static string DocumentationBaseUrl
    {
        get => _documentationBaseUrl ?? throw new InvalidOperationException($"Call {nameof(SetDocumentationBaseUrl)} first to set the documentation base URL.");
    }

    internal static void SetDocumentationBaseUrl(string version)
    {
        if (_documentationBaseUrl is not null)
        {
            throw new InvalidOperationException("Documentation base URL can only be set once");
        }

        string? docVersion = "en/main/";
        const string defaultDevelopmentVersion = "33.33";
        if (!string.IsNullOrWhiteSpace(version) && !version.StartsWith(defaultDevelopmentVersion))
        {
            // We expect version to be something starting with "X.Y" (ignore patch versions)
            Match match = VersionRegex.Match(version);
            if (match.Success)
            {
                docVersion = $"en/release-{match.Groups["major"]}.{match.Groups["minor"]}/";
            }
        }

        _documentationBaseUrl = $"https://git-extensions-documentation.readthedocs.org/{docVersion}";
    }

    public static string AutoNormaliseSymbol
    {
        // when persisted "" is treated as null, so use "+" instead
        get
        {
            string value = GetString("AutoNormaliseSymbol", "_");
            return value == "+" ? "" : value;
        }
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                value = "+";
            }

            SetString("AutoNormaliseSymbol", value);
        }
    }

    public static string FileEditorCommand => @$"""{AppSettings.GetGitExtensionsFullPath()}"" fileeditor";

    public static void UsingContainer(DistributedSettings settingsContainer, Action action)
    {
        SettingsContainer.LockedAction(() =>
            {
                DistributedSettings oldSC = SettingsContainer;
                try
                {
                    SettingsContainer = settingsContainer;
                    action();
                }
                finally
                {
                    SettingsContainer = oldSC;

                    // refresh settings if needed
                    SettingsContainer.GetString(string.Empty, null);
                }
            });
    }

    public static string? GetInstallDir()
    {
        if (IsPortable())
        {
            return GetGitExtensionsDirectory();
        }

        string dir = ReadStringRegValue("InstallDir", string.Empty);
        if (string.IsNullOrEmpty(dir))
        {
            return GetGitExtensionsDirectory();
        }

        return dir;
    }

    public static string? GetResourceDir()
    {
#if DEBUG
        string gitExtDir = GetGitExtensionsDirectory()!.TrimEnd('\\').TrimEnd('/');
        const string debugPath = @"GitExtensions\bin\Debug";
        int len = debugPath.Length;
        if (gitExtDir.Length > len)
        {
            string path = gitExtDir[^len..];

            if (debugPath.ToPosixPath() == path.ToPosixPath())
            {
                string projectPath = gitExtDir[..^len];
                return Path.Join(projectPath, "Bin");
            }
        }
#endif
        return GetInstallDir();
    }

    // for repair only
    public static void SetInstallDir(string dir)
    {
        WriteStringRegValue("InstallDir", dir);
    }

    #region Registry helpers

    private static bool ReadBoolRegKey(string key, bool defaultValue)
    {
        object? obj = VersionIndependentRegKey.GetValue(key);
        if (obj is not string)
        {
            obj = null;
        }

        if (obj is null)
        {
            return defaultValue;
        }

        return ((string)obj).Equals("true", StringComparison.CurrentCultureIgnoreCase);
    }

    private static void WriteBoolRegKey(string key, bool value)
    {
        VersionIndependentRegKey.SetValue(key, value ? "true" : "false");
    }

    [return: NotNullIfNotNull(nameof(defaultValue))]
    private static string? ReadStringRegValue(string key, string? defaultValue)
    {
        return (string?)VersionIndependentRegKey.GetValue(key, defaultValue);
    }

    private static void WriteStringRegValue(string key, string value)
    {
        VersionIndependentRegKey.SetValue(key, value);
    }

    #endregion

    public static bool CheckSettings
    {
        get => ReadBoolRegKey("CheckSettings", true);
        set => WriteBoolRegKey("CheckSettings", value);
    }

    public static string CascadeShellMenuItems
    {
        get => ReadStringRegValue("CascadeShellMenuItems", "110111000111111111");
        set => WriteStringRegValue("CascadeShellMenuItems", value);
    }

    public static string SshPath
    {
        get => ReadStringRegValue("gitssh", "");
        set => WriteStringRegValue("gitssh", value);
    }

    public static bool AlwaysShowAllCommands
    {
        get => ReadBoolRegKey("AlwaysShowAllCommands", false);
        set => WriteBoolRegKey("AlwaysShowAllCommands", value);
    }

    public static bool ShowCurrentBranchInVisualStudio
    {
        // This setting MUST be set to false by default, otherwise it will not work in Visual Studio without
        // other changes in the Visual Studio plugin itself.
        get => ReadBoolRegKey("ShowCurrentBranchInVS", true);
        set => WriteBoolRegKey("ShowCurrentBranchInVS", value);
    }

    public static string GitCommandValue
    {
        get
        {
            if (IsPortable())
            {
                return GetString("gitcommand", "");
            }
            else
            {
                return ReadStringRegValue("gitcommand", "");
            }
        }
        set
        {
            if (GitCommandValue == value)
            {
                return;
            }

            if (IsPortable())
            {
                SetString("gitcommand", value);
            }
            else
            {
                WriteStringRegValue("gitcommand", value);
            }

            GitVersion.ResetVersion();
        }
    }

    public static string GitCommand
    {
        get
        {
            if (string.IsNullOrEmpty(GitCommandValue))
            {
                return "git";
            }

            return GitCommandValue;
        }
    }

    public static bool CommitInfoShowContainedInBranches => CommitInfoShowContainedInBranchesLocal ||
                                                            CommitInfoShowContainedInBranchesRemote ||
                                                            CommitInfoShowContainedInBranchesRemoteIfNoLocal;

    #region Avatars

    public static string AvatarImageCachePath => Path.Join(LocalApplicationDataPath.Value!, "Images");

    public static AvatarFallbackType AvatarFallbackType
    {
        get => GetEnumViaString("GravatarDefaultImageType", AvatarFallbackType.AuthorInitials);
        set => SetString("GravatarDefaultImageType", value.ToString());
    }

    /// <summary>
    /// Gets the size of the commit author avatar. Set to 80px.
    /// </summary>
    /// <remarks>The value should be scaled with DPI.</remarks>
    public static int AuthorImageSizeInCommitInfo => 80;

    public static AvatarProvider AvatarProvider
    {
        get => GetEnumViaString("Appearance.AvatarProvider", AvatarProvider.None);
        set => SetString("Appearance.AvatarProvider", value.ToString());
    }

    // Currently not configurable in UI (Set manually in settings file)
    // Names from here: https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.brushes?view=windowsdesktop-7.0
    // or #AARRGGBB code
    public static string AvatarAuthorInitialsPalette
    {
        get => GetString("Appearance.AvatarAuthorInitialsPalette", "SlateGray,RoyalBlue,Purple,OrangeRed,Teal,OliveDrab");
        set => SetString("Appearance.AvatarAuthorInitialsPalette", value);
    }

    // Currently not configurable in UI (Set manually in settings file)
    public static float AvatarAuthorInitialsLuminanceThreshold => GetFloat("AvatarAuthorInitialsLuminanceThreshold", 0.5f);

    /// <summary>
    /// Loads a setting with GetString and parses it to an enum
    /// </summary>
    /// <remarks>
    /// It's currently a limitation by <see cref="SettingsCache"/> that a given setting can
    /// only ever use GetString/SetString or GetEnum/SetEnum but not both. This is the case
    /// because <see cref="SettingsCache"/> caches a typed/parsed value of the setting and
    /// crashes at <see cref="SettingsCache.TryGetValue(string, out string?)"/>
    /// if the type that is requested doesn't match the cached type.
    /// </remarks>
    private static TEnum GetEnumViaString<TEnum>(string settingName, TEnum defaultValue)
        where TEnum : struct
    {
        string? settingStringValue = GetString(settingName, defaultValue.ToString());

        if (Enum.TryParse(settingStringValue, out TEnum settingEnumValue))
        {
            return settingEnumValue;
        }

        return defaultValue;
    }

    private static void MigrateAvatarSettings()
    {
        // Load settings as strings to support obsolete settings that are no longer
        // part of the enums AvatarProvider or AvatarFallbackType.

        string provider = GetString("Appearance.AvatarProvider", "Default");

        // if the provider turns out to be the obsolete "author initials" we can skip loading
        // the fallback image, because it will be overwritten anyways, so loading it is postponed.
        string fallbackImage;

        bool providerChanged = false;
        bool fallbackImageChanged = false;

        if (provider == "AuthorInitials")
        {
            provider = AvatarProvider.None.ToString();
            fallbackImage = AvatarFallbackType.AuthorInitials.ToString();

            providerChanged = true;
            fallbackImageChanged = true;
        }
        else
        {
            if (provider == "Gravatar")
            {
                provider = AvatarProvider.Default.ToString();
                providerChanged = true;
            }

            // if provider was not "AuthorInitials" the fallback image
            // is loaded to check if it has to be migrated.
            fallbackImage = GetString("GravatarDefaultImageType", "AuthorInitials");

            if (fallbackImage == "None")
            {
                fallbackImage = AvatarFallbackType.AuthorInitials.ToString();
                fallbackImageChanged = true;
            }
        }

        if (providerChanged)
        {
            SetString("Appearance.AvatarProvider", provider);
        }

        if (fallbackImageChanged)
        {
            SetString("GravatarDefaultImageType", fallbackImage);
        }

        if (providerChanged || fallbackImageChanged)
        {
            SaveSettings();
        }
    }

    private static void MigrateSshSettings()
    {
        string ssh = AppSettings.SshPath;
        if (!string.IsNullOrEmpty(ssh))
        {
            // OpenSSH uses empty path, compatibility with path set in 3.4
            string? path = new SshPathLocator().GetSshFromGitDir(LinuxToolsDir);
            if (path == ssh)
            {
                AppSettings.SshPath = "";
            }
        }
    }

    /// <summary>
    /// Migrate editor settings if GE was installed in 'Program Files (x86)' before 4.3.
    /// Only check and update global Git settings in Windows, the only set by GE.
    /// Guess previous, migrate to current path (i.e. the first usage).
    /// Only needed in x64, should be meaningless on other platforms.
    /// </summary>
    private static void MigrateEditorSettings()
    {
        // Only run this once
        bool isMigrated = IsEditorSettingsMigrated.Value;
        IsEditorSettingsMigrated.Value = true;
        if (isMigrated)
        {
            return;
        }

        EnvironmentConfiguration.SetEnvironmentVariables();
        IPersistentConfigValueStore globalSettings = new GitConfigSettings(new Executable(AppSettings.GitCommand), GitSettingLevel.Global);
        string? path = globalSettings.GetValue("core.editor");
        if (path?.Contains("Program Files (x86)/GitExtensions", StringComparison.CurrentCultureIgnoreCase) is not true)
        {
            return;
        }

#if DEBUG
        // avoid setting, this may be in the debugger
        throw new Exception($"Update the core.editor path {path} in global git settings");
#else
        globalSettings.SetValue("core.editor", FileEditorCommand.ConvertPathToGitSetting());
        globalSettings.Save();
#endif
    }

    #endregion

    private static string? _currentTranslation;

    public static string? CurrentTranslation
    {
        get => _currentTranslation ?? Translation;
        set => _currentTranslation = value;
    }

    private static readonly Dictionary<string, string> _languageCodes =
        new(StringComparer.InvariantCultureIgnoreCase)
        {
            { "Czech", "cs" },
            { "Dutch", "nl" },
            { "English", "en" },
            { "French", "fr" },
            { "German", "de" },
            { "Indonesian", "id" },
            { "Italian", "it" },
            { "Japanese", "ja" },
            { "Korean", "ko" },
            { "Polish", "pl" },
            { "Portuguese (Brazil)", "pt-BR" },
            { "Portuguese (Portugal)", "pt-PT" },
            { "Romanian", "ro" },
            { "Russian", "ru" },
            { "Simplified Chinese", "zh-CN" },
            { "Spanish", "es" },
            { "Traditional Chinese", "zh-TW" }
        };

    public static string CurrentLanguageCode
    {
        get
        {
            if (CurrentTranslation is not null && _languageCodes.TryGetValue(CurrentTranslation, out string? code))
            {
                return code;
            }

            return "en";
        }
    }

    public static CultureInfo CurrentCultureInfo
    {
        get
        {
            try
            {
                return CultureInfo.GetCultureInfo(CurrentLanguageCode);
            }
            catch (CultureNotFoundException)
            {
                Debug.WriteLine("Culture {0} not found", [CurrentLanguageCode]);
                return CultureInfo.GetCultureInfo("en");
            }
        }
    }

    public static readonly Dictionary<string, Encoding> AvailableEncodings = [];

    /// <summary>
    /// Gets or sets the default pull action that is performed by the toolbar icon when it is clicked on.
    /// </summary>
    public static GitPullAction DefaultPullAction
    {
        get => GetEnum("DefaultPullAction", GitPullAction.Merge);
        set => SetEnum("DefaultPullAction", value);
    }

    /// <summary>
    /// Gets or sets the default pull action as configured in the FormPull dialog.
    /// </summary>
    public static GitPullAction FormPullAction
    {
        get => GetEnum("FormPullAction", GitPullAction.Merge);
        set => SetEnum("FormPullAction", value);
    }

    public static GitPullAction? AutoPullOnPushRejectedAction
    {
        get => GetNullableEnum<GitPullAction>("AutoPullOnPushRejectedAction");
        set => SetNullableEnum("AutoPullOnPushRejectedAction", value);
    }

    public static string Dictionary
    {
        get => SettingsContainer.Detached().Dictionary;
        set => SettingsContainer.Detached().Dictionary = value;
    }

    // History Compatibility: The meaning of this value is changed in the GUI, setting name is kept for compatibility
    public static bool HideMergeCommits
    {
        get => !GetBool("showmergecommits", true);
        set => SetBool("showmergecommits", !value);
    }

    public static string[] RevisionFilterDropdowns
    {
        get => GetString("RevisionFilterDropdowns", string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        set => SetString("RevisionFilterDropdowns", string.Join("\n", value ?? []));
    }

    /// <summary>Gets or sets the path to the GNU/Linux tools (bash, ps, sh, ssh, etc.), e.g. "C:\Program Files\Git\usr\bin\"</summary>
    public static string LinuxToolsDir
    {
        // History Compatibility: Migrate the setting value from the from the former "gitbindir"
        get => GetString(nameof(LinuxToolsDir), defaultValue: null) ?? GetString("gitbindir", "");
        set
        {
            string linuxToolsDir = value.EnsureTrailingPathSeparator();
            SetString(nameof(LinuxToolsDir), linuxToolsDir);
        }
    }

    /// <summary>
    ///  Max file size for the diff view in MB. 0 means unlimited. Values are
    ///  clamped to 0..100 on write and on read so that an invalid persisted
    ///  value cannot break the diff viewer.
    /// </summary>
    public static int MaxDiffFileSizeMb
    {
        get => Math.Clamp(GetInt("maxdifffilesize", 5), 0, 100);
        set => SetInt("maxdifffilesize", Math.Clamp(value, 0, 100));
    }

    [MaybeNull]
    public static string RecentWorkingDir
    {
        get => GetString("RecentWorkingDir", null);
        set => SetString("RecentWorkingDir", value);
    }

    public static string Plink
    {
        get => GetString("plink", Environment.GetEnvironmentVariable("GITEXT_PLINK") ?? ReadStringRegValue("plink", ""));
        set
        {
            if (value != Environment.GetEnvironmentVariable("GITEXT_PLINK"))
            {
                SetString("plink", value);
            }
        }
    }

    public static string Puttygen
    {
        get => GetString("puttygen", Environment.GetEnvironmentVariable("GITEXT_PUTTYGEN") ?? ReadStringRegValue("puttygen", ""));
        set
        {
            if (value != Environment.GetEnvironmentVariable("GITEXT_PUTTYGEN"))
            {
                SetString("puttygen", value);
            }
        }
    }

    /// <summary>Gets the path to Pageant (SSH auth agent).</summary>
    public static string Pageant
    {
        get => GetString("pageant", Environment.GetEnvironmentVariable("GITEXT_PAGEANT") ?? ReadStringRegValue("pageant", ""));
        set
        {
            if (value != Environment.GetEnvironmentVariable("GITEXT_PAGEANT"))
            {
                SetString("pageant", value);
            }
        }
    }

    public static ThemeId ThemeId
    {
        get
        {
            return new ThemeId(
                GetString("uitheme_v2", ThemeId.DefaultLight.Name),
                GetBool("uithemeisbuiltin_v2", ThemeId.DefaultLight.IsBuiltin));
        }
        set
        {
            SetString("uitheme_v2", value.Name ?? string.Empty);
            SetBool("uithemeisbuiltin_v2", value.IsBuiltin);
        }
    }

    public static string[] ThemeVariations
    {
        get
        {
            return GetString("uithemevariations", string.Empty).Split(Delimiters.Comma, StringSplitOptions.RemoveEmptyEntries);
        }
        set
        {
            SetString("uithemevariations", string.Join(",", value ?? []));
        }
    }

    #region Fonts

    public static Font FixedWidthFont
    {
        get => GetFont("difffont", new Font("Consolas", 10));
        set => SetFont("difffont", value);
    }

    public static Font CommitFont
    {
        get => GetFont("commitfont", SystemFonts.MessageBoxFont!);
        set => SetFont("commitfont", value);
    }

    public static Font MonospaceFont
    {
        get => GetFont("monospacefont", new Font("Consolas", 9));
        set => SetFont("monospacefont", value);
    }

    public static Font Font
    {
        get => GetFont("font", SystemFonts.MessageBoxFont!);
        set => SetFont("font", value);
    }

    public static Font? ConEmuConsoleFont
    {
        get => GetFont("conemuconsolefont", null);
        set => SetFont("conemuconsolefont", value);
    }

    #endregion

    /// <summary>
    /// Diff appearance, alternatives to "patch" viewer.
    /// </summary>
    public static int NumberOfContextLines
    {
        get
        {
            const int defaultValue = 3;
            return RememberNumberOfContextLines ? GetInt("NumberOfContextLines", defaultValue) : defaultValue;
        }
        set
        {
            if (RememberNumberOfContextLines)
            {
                SetInt("NumberOfContextLines", value);
            }
        }
    }

    public static string GetDictionaryDir()
    {
        return Path.Join(GetResourceDir()!, "Dictionaries");
    }

    public static void SaveSettings()
    {
        SaveEncodings();
        try
        {
            SettingsContainer.LockedAction(() =>
            {
                // prepend "Global\" in order to be safe in preparation for non-Windows OS, too
                _globalMutex ??= new Mutex(initiallyOwned: false, name: @$"Global\Mutex{SettingsFilePath.ToPosixPath()}");

                try
                {
                    _globalMutex.WaitOne();
                    SettingsContainer.Save();
                }
                finally
                {
                    _globalMutex.ReleaseMutex();
                }
            });

            Saved?.Invoke();
        }
        catch
        {
        }
    }

    public static void LoadSettings()
    {
        LoadEncodings();

        try
        {
            // Set environment variable
            GitSshHelpers.SetGitSshEnvironmentVariable(SshPath);
            MigrateEditorSettings();
        }
        catch
        {
        }
    }

    public static string? OwnScripts
    {
        get => GetString("ownScripts", "");
        set => SetString("ownScripts", value ?? "");
    }

    public static ShorteningRecentRepoPathStrategy ShorteningRecentRepoPathStrategy
    {
        get => GetEnum("ShorteningRecentRepoPathStrategy", ShorteningRecentRepoPathStrategy.MostSignDir);
        set => SetEnum("ShorteningRecentRepoPathStrategy", value);
    }

    [MaybeNull]
    public static string SerializedHotkeys
    {
        get => GetString("SerializedHotkeys", null);
        set => SetString("SerializedHotkeys", value);
    }

    public static string CommitValidationRegEx
    {
        get => GetString("CommitValidationRegEx", string.Empty);
        set => SetString("CommitValidationRegEx", value);
    }

    public static string CommitTemplates
    {
        get => GetString("CommitTemplates", string.Empty);
        set => SetString("CommitTemplates", value);
    }

    public static DateTime LastUpdateCheck
    {
        get => GetDate("LastUpdateCheck", default);
        set => SetDate("LastUpdateCheck", value);
    }

    public static GitRefsSortBy RefsSortBy
    {
        get => GetEnum("RefsSortBy", GitRefsSortBy.Default);
        set => SetEnum("RefsSortBy", value);
    }

    public static GitRefsSortOrder RefsSortOrder
    {
        get => GetEnum("RefsSortOrder", GitRefsSortOrder.Descending);
        set => SetEnum("RefsSortOrder", value);
    }

    public static DiffListSortType DiffListSorting
    {
        get => GetEnum("DiffListSortType", DiffListSortType.FilePath);
        set => SetEnum("DiffListSortType", value);
    }

    public static string GetGitExtensionsFullPath()
    {
#if DEBUG
        if (!IsDesignMode)
        {
            bool isExpectedExe =

                // The app's entry point is GitExtensions.exe
                _applicationExecutablePath.EndsWith("GitExtensions.exe", StringComparison.InvariantCultureIgnoreCase) ||

                // Tests are run by testhost.exe
                _applicationExecutablePath.EndsWith("testhost.exe", StringComparison.InvariantCultureIgnoreCase) ||
                _applicationExecutablePath.EndsWith("testhost.x86.exe", StringComparison.InvariantCultureIgnoreCase) ||

                _applicationExecutablePath.EndsWith("ReSharperTestRunner.exe", StringComparison.InvariantCultureIgnoreCase) ||
                _applicationExecutablePath.EndsWith("dotnet.exe", StringComparison.InvariantCultureIgnoreCase) ||

                // Translations
                _applicationExecutablePath.EndsWith("TranslationApp.exe", StringComparison.InvariantCultureIgnoreCase);

            DebugHelpers.Assert(isExpectedExe, $"{_applicationExecutablePath} must point to GitExtensions.exe");
        }
#endif

        return _applicationExecutablePath;
    }

    public static string? GetGitExtensionsDirectory()
    {
        return Path.GetDirectoryName(GetGitExtensionsFullPath());
    }

    private static RegistryKey? _versionIndependentRegKey;

    private static RegistryKey VersionIndependentRegKey
    {
        get
        {
            if (_versionIndependentRegKey is null)
            {
                _versionIndependentRegKey = Registry.CurrentUser.CreateSubKey("Software\\GitExtensions", RegistryKeyPermissionCheck.ReadWriteSubTree);
                Validates.NotNull(_versionIndependentRegKey);
            }

            return _versionIndependentRegKey;
        }
    }

    public static string PrioritizedBranchNames
    {
        get => GetString("PrioritizedBranchNames", "main[^/]*|master[^/]*|release/.*");
        set => SetString("PrioritizedBranchNames", value);
    }

    public static string PrioritizedRemoteNames
    {
        get => GetString("PrioritizedRemoteNames", "origin|upstream");
        set => SetString("PrioritizedRemoteNames", value);
    }

    /// <summary>
    ///  Remote names to prefer when auto-detecting build server integration, separated by <c>|</c>.
    ///  Defaults to <c>upstream|origin</c> so that forks resolve to the upstream project's CI.
    /// </summary>
    public static string PrioritizedBuildServerRemoteNames
    {
        get => GetString("PrioritizedBuildServerRemoteNames", "upstream|origin|remote");
        set => SetString("PrioritizedBuildServerRemoteNames", value);
    }

    public static IEnumerable<string> CustomGenericRemoteNames
    {
        get => GetString("CustomGenericRemoteNames", string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries);
    }

    // There is a bug in .NET/.NET Designer that fails to execute Properties.Settings.Default call.
    // Return false whilst we're in the designer.
    public static bool IsPortable() => !IsDesignMode && Properties.Settings.Default.IsPortable;

    // Currently not configurable in UI (Set manually in settings file)
    public static bool WriteErrorLog
    {
        get => GetBool("WriteErrorLog", false);
    }

    // Currently not configurable in UI (Set manually in settings file)
    public static bool WorkaroundActivateFromMinimize
    {
        get => GetBool("WorkaroundActivateFromMinimize", false);
    }

    public static bool GitAsyncWhenMinimized
    {
        get => GetBool("GitAsyncWhenMinimized", false);
    }

    private static IEnumerable<(string name, string? value)> GetSettingsFromRegistry()
    {
        RegistryKey? oldSettings = VersionIndependentRegKey.OpenSubKey("GitExtensions");

        if (oldSettings is null)
        {
            yield break;
        }

        foreach (string name in oldSettings.GetValueNames())
        {
            object? value = oldSettings.GetValue(name, null);

            if (value is not null)
            {
                yield return (name, value.ToString());
            }
        }
    }

    private static void ImportFromRegistry()
    {
        SettingsContainer.SettingsCache.Import(GetSettingsFromRegistry());
    }

    #region Save in settings file

    // String
    [return: NotNullIfNotNull(nameof(defaultValue))]
    public static string? GetString(string name, string? defaultValue) => SettingsContainer.GetString(name, defaultValue);
    public static void SetString(string name, string value) => SettingsContainer.SetString(name, value);

    // Bool
    public static bool? GetBool(string name) => SettingsContainer.GetBool(name);
    public static bool GetBool(string name, bool defaultValue) => SettingsContainer.GetBool(name, defaultValue);
    public static void SetBool(string name, bool? value) => SettingsContainer.SetBool(name, value);

    // Int
    public static int? GetInt(string name) => SettingsContainer.GetInt(name);
    public static int GetInt(string name, int defaultValue) => SettingsContainer.GetInt(name, defaultValue);
    public static void SetInt(string name, int? value) => SettingsContainer.SetInt(name, value);

    // Float
    public static float GetFloat(string name, float defaultValue) => SettingsContainer.GetFloat(name, defaultValue);

    // Date
    public static DateTime? GetDate(string name) => SettingsContainer.GetDate(name);
    public static DateTime GetDate(string name, DateTime defaultValue) => SettingsContainer.GetDate(name, defaultValue);
    public static void SetDate(string name, DateTime? value) => SettingsContainer.SetDate(name, value);

    // Font
    [return: NotNullIfNotNull("defaultValue")]
    public static Font? GetFont(string name, Font? defaultValue) => SettingsContainer.GetFont(name, defaultValue);
    public static void SetFont(string name, Font? value) => SettingsContainer.SetFont(name, value);

    [Obsolete("AppSettings is no longer responsible for colors, ThemeModule is. Only used by ThemeMigration.")]
    public static Color GetColor(AppColor name)
    {
        return SettingsContainer.GetColor(name.ToString().ToLowerInvariant() + "color", AppColorDefaults.GetBy(name));
    }

    // Enum
    public static T GetEnum<T>(string name, T defaultValue) where T : struct, Enum => SettingsContainer.GetEnum(name, defaultValue);
    public static void SetEnum<T>(string name, T value) where T : Enum => SettingsContainer.SetEnum(name, value);

    public static T? GetNullableEnum<T>(string name) where T : struct, Enum => ((ISettingsValueGetter)SettingsContainer).GetValue<T>(name);
    public static void SetNullableEnum<T>(string name, T? value) where T : struct, Enum => SettingsContainer.SetValue(name, value?.ToString());
    #endregion

    private static void LoadEncodings()
    {
        void AddEncoding(Encoding e)
        {
            AvailableEncodings[e.WebName] = e;
        }

        void AddEncodingByName(string s)
        {
            try
            {
                AddEncoding(Encoding.GetEncoding(s));
            }
            catch
            {
            }
        }

        string availableEncodings = GetString("AvailableEncodings", "");
        if (string.IsNullOrWhiteSpace(availableEncodings))
        {
            // Default encoding set
            AddEncoding(Encoding.Default);
            AddEncoding(new ASCIIEncoding());
            AddEncoding(new UnicodeEncoding());

            // UTF-7 is no longer supported, see: https://github.com/dotnet/docs/issues/19274
            // AddEncoding(new UTF7Encoding());

            AddEncoding(new UTF8Encoding(false));

            try
            {
                AddEncoding(Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage));
            }
            catch
            {
                // there are CultureInfo values without a code page
            }

            try
            {
                AddEncoding(Encoding.GetEncoding(0));
            }
            catch
            {
                // catch if error retrieving operating system's active code page
            }
        }
        else
        {
            UTF8Encoding utf8 = new(false);
            foreach (string encodingName in availableEncodings.LazySplit(';'))
            {
#pragma warning disable SYSLIB0001 // Type or member is obsolete
                if (encodingName == Encoding.UTF7.WebName)
#pragma warning restore SYSLIB0001 // Type or member is obsolete
                {
                    // UTF-7 is no longer supported, see: https://github.com/dotnet/docs/issues/19274
                    continue;
                }

                // create utf-8 without BOM
                if (encodingName == utf8.WebName)
                {
                    AddEncoding(utf8);
                }

                // default encoding
                else if (encodingName == "Default")
                {
                    AddEncoding(Encoding.Default);
                }

                // add encoding by name
                else
                {
                    AddEncodingByName(encodingName);
                }
            }
        }
    }

    private static void SaveEncodings()
    {
        string availableEncodings = AvailableEncodings.Values.Select(e => e.WebName).Join(";");
        availableEncodings = availableEncodings.Replace(Encoding.Default.WebName, "Default");
        SetString("AvailableEncodings", availableEncodings);
    }

    private static bool? _isDesignMode;

    private static bool IsDesignMode
    {
        get
        {
            if (_isDesignMode is null)
            {
                string processName = Path.GetFileNameWithoutExtension(Environment.ProcessPath ?? string.Empty);
                _isDesignMode = processName.Contains("devenv", StringComparison.OrdinalIgnoreCase) || processName.Contains("designtoolsserver", StringComparison.OrdinalIgnoreCase);
            }

            return _isDesignMode.Value;
        }
    }

    internal static TestAccessor GetTestAccessor() => new();

    internal struct TestAccessor
    {
        public readonly string ApplicationExecutablePath
        {
            get => _applicationExecutablePath;
            set => _applicationExecutablePath = value;
        }

        public readonly Lazy<string?> ApplicationDataPath
        {
            get => AppSettings.ApplicationDataPath;
            set => AppSettings.ApplicationDataPath = value;
        }

        public readonly void ResetDocumentationBaseUrl() => AppSettings._documentationBaseUrl = null;
    }
}

public class AppSettingsPath : SettingsPath
{
    public AppSettingsPath(string pathName) : base(null, pathName)
    {
    }

    public AppSettingsPath(SettingsPath parent, string pathName) : base(null, parent.PathFor(pathName))
    {
    }

    public override string? GetValue(string name)
    {
        return AppSettings.SettingsContainer.GetValue(PathFor(name));
    }

    public override void SetValue(string name, string? value)
    {
        AppSettings.SettingsContainer.SetValue(PathFor(name), value);
    }
}
