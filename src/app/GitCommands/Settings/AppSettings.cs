// Internal callers still use the legacy Get/Set methods for properties that can't yet be converted to ISetting<T>.
#pragma warning disable CS0618 // Type or member is obsolete

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
    public static string SettingsFilePath => Path.Combine(ApplicationDataPath.Value!, SettingsFileName);
    public static string UserPluginsPath => Path.Combine(LocalApplicationDataPath.Value!, UserPluginsDirectoryName);

    public static DistributedSettings SettingsContainer { get; private set; }

    private static readonly SettingsPath AppearanceSettingsPath = new AppSettingsPath("Appearance");
    private static readonly SettingsPath ConfirmationsSettingsPath = new AppSettingsPath("Confirmations");
    private static readonly SettingsPath DetailedSettingsPath = new AppSettingsPath("Detailed");
    private static readonly SettingsPath DialogSettingsPath = new AppSettingsPath("Dialogs");
    private static readonly SettingsPath ExperimentalSettingsPath = new AppSettingsPath(DetailedSettingsPath, "Experimental");
    private static readonly SettingsPath FileStatusSettingsPath = new AppSettingsPath(AppearanceSettingsPath, "FileStatus");
    private static readonly SettingsPath RevisionGraphSettingsPath = new AppSettingsPath(AppearanceSettingsPath, "RevisionGraph");
    private static readonly SettingsPath RecentRepositories = new AppSettingsPath("RecentRepositories");
    private static readonly SettingsPath RootSettingsPath = new AppSettingsPath(pathName: "");
    private static readonly SettingsPath HiddenSettingsPath = new AppSettingsPath("Hidden");
    private static readonly SettingsPath MigrationSettingsPath = new AppSettingsPath(HiddenSettingsPath, "Migration");

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

            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ApplicationId);
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

    public static ISetting<bool?> TelemetryEnabled { get; } = Setting.Create<bool>(RootSettingsPath, "TelemetryEnabled");

    public static ISetting<bool> AutoNormaliseBranchName { get; } = Setting.Create(RootSettingsPath, "AutoNormaliseBranchName", defaultValue: true);

    // When persisted, "" is treated as null, so "+" is stored instead.
    public static ISetting<string> AutoNormaliseSymbol { get; } = Setting.CreateIntercepted<string, string>(
        RootSettingsPath,
        "AutoNormaliseSymbol",
        defaultValue: "_",
        read: v => v == "+" ? "" : v,
        store: v => string.IsNullOrWhiteSpace(v) ? "+" : v);

    public static string FileEditorCommand => @$"""{AppSettings.GetGitExtensionsFullPath()}"" fileeditor";

    public static ISetting<bool> RememberAmendCommitState { get; } = Setting.Create(RootSettingsPath, "RememberAmendCommitState", defaultValue: true);

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
                return Path.Combine(projectPath, "Bin");
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

    public static ISetting<int> FileStatusFindInFilesGitGrepTypeIndex { get; } = Setting.Create(FileStatusSettingsPath, nameof(FileStatusFindInFilesGitGrepTypeIndex), defaultValue: 1);

    public static ISetting<bool> FileStatusMergeSingleItemWithFolder { get; } = Setting.Create(FileStatusSettingsPath, nameof(FileStatusMergeSingleItemWithFolder), defaultValue: false);

    public static ISetting<bool> FileStatusShowGroupNodesInFlatList { get; } = Setting.Create(FileStatusSettingsPath, nameof(FileStatusShowGroupNodesInFlatList), defaultValue: false);

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

    // Currently not configurable in UI (Set manually in settings file)
    public static ISetting<bool> WslGitEnabled { get; } = Setting.Create(RootSettingsPath, "WslGitEnabled", defaultValue: true);

    // Currently not configurable in UI (Set manually in settings file)
    public static ISetting<string> WslCommand { get; } = Setting.Create(RootSettingsPath, nameof(WslCommand), defaultValue: "wsl");

    // Currently not configurable in UI (Set manually in settings file)
    public static ISetting<string> WslGitCommand { get; } = Setting.Create(RootSettingsPath, nameof(WslGitCommand), defaultValue: "git");

    public static ISetting<bool> StashKeepIndex { get; } = Setting.Create(RootSettingsPath, "stashkeepindex", defaultValue: false);

    // History Compatibility: The setting was originally called 'StashConfirmDropShow', and then it was inverted.
    // To maintain compat with existing user settings, negate the stored value.
    public static ISetting<bool> DontConfirmStashDrop { get; } = Setting.CreateIntercepted<bool, bool>(
        RootSettingsPath,
        "stashconfirmdropshow",
        defaultValue: false,
        read: v => !v,
        store: v => !v);

    public static ISetting<bool> ApplyPatchIgnoreWhitespace { get; } = Setting.Create(RootSettingsPath, "applypatchignorewhitespace", defaultValue: false);

    public static ISetting<bool> ApplyPatchSignOff { get; } = Setting.Create(RootSettingsPath, "applypatchsignoff", defaultValue: true);

    // History Compatibility: The settings key has patience in the name for historical reasons
    public static ISetting<bool> UseHistogramDiffAlgorithm { get; } = Setting.Create(RootSettingsPath, "usepatiencediffalgorithm", defaultValue: false);

    /// <summary>
    /// Use Git coloring for selected commands
    /// </summary>
    public static ISetting<bool> UseGitColoring { get; } = Setting.Create(AppearanceSettingsPath, nameof(UseGitColoring), defaultValue: true);

    /// <summary>
    /// Color the background at changes (invert colors).
    /// </summary>
    public static ISetting<bool> ReverseGitColoring { get; } = Setting.Create(AppearanceSettingsPath, nameof(ReverseGitColoring), defaultValue: true);

    public static ISetting<bool> ShowErrorsWhenStagingFiles { get; } = Setting.Create(RootSettingsPath, "showerrorswhenstagingfiles", defaultValue: true);

    public static ISetting<bool> EnsureCommitMessageSecondLineEmpty { get; } = Setting.Create(RootSettingsPath, "addnewlinetocommitmessagewhenmissing", defaultValue: true);

    public static ISetting<string> LastCommitMessage { get; } = Setting.Create(RootSettingsPath, "lastCommitMessage", defaultValue: "");

    public static ISetting<int> CommitDialogNumberOfPreviousMessages { get; } = Setting.Create(RootSettingsPath, "commitDialogNumberOfPreviousMessages", defaultValue: 6);

    public static ISetting<bool> CommitDialogSelectStagedOnEnterMessage { get; } = Setting.Create(DialogSettingsPath, nameof(CommitDialogSelectStagedOnEnterMessage), defaultValue: true);

    public static ISetting<bool> CommitDialogShowOnlyMyMessages { get; } = Setting.Create(RootSettingsPath, "commitDialogShowOnlyMyMessages", defaultValue: false);

    public static ISetting<bool> ShowCommitAndPush { get; } = Setting.Create(RootSettingsPath, "showcommitandpush", defaultValue: true);

    public static ISetting<bool> ShowResetWorkTreeChanges { get; } = Setting.Create(RootSettingsPath, "showresetunstagedchanges", defaultValue: true);

    public static ISetting<bool> ShowResetAllChanges { get; } = Setting.Create(RootSettingsPath, "showresetallchanges", defaultValue: true);

    public static ISetting<bool> ShowConEmuTab { get; } = Setting.Create(DetailedSettingsPath, nameof(ShowConEmuTab), defaultValue: true);

    private const string ConEmuStyleDefault = "Default";
    private const string ConEmuStyleDark = "<Tomorrow Night>";
    private const string ConEmuStyleLight = "<Tomorrow>";

    public static ISetting<string> ConEmuStyle { get; } = Setting.Create(DetailedSettingsPath, nameof(ConEmuStyle), ConEmuStyleDefault);

    /// <summary>
    ///  Returns the ConEmu style to use. When the configured value is <see cref="ConEmuStyleDefault"/>,
    ///  automatically selects a style that matches the current application theme.
    /// </summary>
    public static string GetEffectiveConEmuStyle()
    {
        string? style = ConEmuStyle.Value;
        return style is null || style == ConEmuStyleDefault
            ? Application.IsDarkModeEnabled ? ConEmuStyleDark : ConEmuStyleLight
            : style;
    }

    public static ISetting<string> ConEmuTerminal { get; } = Setting.Create(DetailedSettingsPath, nameof(ConEmuTerminal), defaultValue: "bash");
    public static ISetting<int> OutputHistoryDepth { get; } = Setting.Create(DetailedSettingsPath, nameof(OutputHistoryDepth), defaultValue: 20);
    public static ISetting<bool> OutputHistoryPanelVisible { get; } = Setting.Create(DetailedSettingsPath, nameof(OutputHistoryPanelVisible), defaultValue: false);
    public static ISetting<bool> ShowOutputHistoryAsTab { get; } = Setting.Create(DetailedSettingsPath, nameof(ShowOutputHistoryAsTab), defaultValue: true);
    public static ISetting<bool> UseBrowseForFileHistory { get; } = Setting.Create(DetailedSettingsPath, nameof(UseBrowseForFileHistory), defaultValue: true);
    public static ISetting<bool> UseDiffViewerForBlame { get; } = Setting.Create(DetailedSettingsPath, nameof(UseDiffViewerForBlame), defaultValue: false);
    public static ISetting<bool> ShowGpgInformation { get; } = Setting.Create(DetailedSettingsPath, nameof(ShowGpgInformation), defaultValue: true);

    public static CommitInfoPosition CommitInfoPosition
    {
        get => ((ISettingsValueGetter)DetailedSettingsPath).GetValue<CommitInfoPosition>("CommitInfoPosition") ?? (
            DetailedSettingsPath.GetBool("ShowRevisionInfoNextToRevisionGrid") == true // legacy setting
                ? CommitInfoPosition.RightwardFromList
                : CommitInfoPosition.BelowList);
        set => DetailedSettingsPath.SetEnum("CommitInfoPosition", value);
    }

    public static ISetting<bool> MessageEditorWordWrap => Setting.Create(DetailedSettingsPath, nameof(MessageEditorWordWrap), defaultValue: false);

    public static ISetting<bool> ShowSplitViewLayout { get; } = Setting.Create(DetailedSettingsPath, nameof(ShowSplitViewLayout), defaultValue: true);

    public static ISetting<bool> ProvideAutocompletion { get; } = Setting.Create(RootSettingsPath, "provideautocompletion", defaultValue: true);

    public static ISetting<TruncatePathMethod> TruncatePathMethod { get; } = Setting.Create(RootSettingsPath, "truncatepathmethod", GitCommands.TruncatePathMethod.None);

    public static ISetting<bool> ShowGitStatusInBrowseToolbar { get; } = Setting.Create(RootSettingsPath, "showgitstatusinbrowsetoolbar", defaultValue: true);

    public static ISetting<bool> ShowGitStatusForArtificialCommits { get; } = Setting.Create(RootSettingsPath, "showgitstatusforartificialcommits", defaultValue: true);

    public static EnumRuntimeSetting<RevisionSortOrder> RevisionSortOrder { get; } = new(RootSettingsPath, nameof(RevisionSortOrder), GitCommands.RevisionSortOrder.GitDefault);

    public static bool CommitInfoShowContainedInBranches => CommitInfoShowContainedInBranchesLocal.Value ||
                                                            CommitInfoShowContainedInBranchesRemote.Value ||
                                                            CommitInfoShowContainedInBranchesRemoteIfNoLocal.Value;

    public static ISetting<bool> CommitInfoShowContainedInBranchesLocal { get; } = Setting.Create(RootSettingsPath, "commitinfoshowcontainedinbrancheslocal", defaultValue: true);

    public static ISetting<bool> CheckForUncommittedChangesInCheckoutBranch { get; } = Setting.Create(RootSettingsPath, "checkforuncommittedchangesincheckoutbranch", defaultValue: true);

    public static ISetting<bool> AlwaysShowCheckoutBranchDlg { get; } = Setting.Create(RootSettingsPath, "AlwaysShowCheckoutBranchDlg", defaultValue: false);

    public static ISetting<bool> CommitAndPushForcedWhenAmend { get; } = Setting.Create(RootSettingsPath, "CommitAndPushForcedWhenAmend", defaultValue: false);

    public static ISetting<bool> CommitInfoShowContainedInBranchesRemote { get; } = Setting.Create(RootSettingsPath, "commitinfoshowcontainedinbranchesremote", defaultValue: false);

    public static ISetting<bool> CommitInfoShowContainedInBranchesRemoteIfNoLocal { get; } = Setting.Create(RootSettingsPath, "commitinfoshowcontainedinbranchesremoteifnolocal", defaultValue: false);

    public static ISetting<bool> CommitInfoShowContainedInTags { get; } = Setting.Create(RootSettingsPath, "commitinfoshowcontainedintags", defaultValue: true);

    public static ISetting<bool> CommitInfoShowTagThisCommitDerivesFrom { get; } = Setting.Create(RootSettingsPath, "commitinfoshowtagthiscommitderivesfrom", defaultValue: true);

    #region Avatars

    public static string AvatarImageCachePath => Path.Combine(LocalApplicationDataPath.Value!, "Images\\");

    // Migrate-on-read: legacy stored values "AuthorInitials" and "Gravatar" are mapped to current enum values.
    public static ISetting<AvatarFallbackType> AvatarFallbackType { get; } = Setting.CreateIntercepted<string, AvatarFallbackType>(
        RootSettingsPath,
        "GravatarDefaultImageType",
        defaultValue: GitCommands.AvatarFallbackType.AuthorInitials,
        read: v => v switch { "None" => GitCommands.AvatarFallbackType.AuthorInitials, _ => Enum.TryParse(v, out AvatarFallbackType e) ? e : GitCommands.AvatarFallbackType.AuthorInitials },
        store: v => v.ToString());

    public static ISetting<string> CustomAvatarTemplate { get; } = Setting.Create(RootSettingsPath, "CustomAvatarTemplate", defaultValue: "");

    /// <summary>
    /// Gets the size of the commit author avatar. Set to 80px.
    /// </summary>
    /// <remarks>The value should be scaled with DPI.</remarks>
    public static int AuthorImageSizeInCommitInfo => 80;

    public static ISetting<int> AvatarImageCacheDays { get; } = Setting.Create(RootSettingsPath, "authorimagecachedays", defaultValue: 13);

    public static ISetting<bool> ShowAuthorAvatarInCommitInfo { get; } = Setting.Create(RootSettingsPath, "showauthorgravatar", defaultValue: true);

    // Migrate-on-read: legacy stored values "AuthorInitials" and "Gravatar" are mapped to current enum values.
    public static ISetting<AvatarProvider> AvatarProvider { get; } = Setting.CreateIntercepted<string, AvatarProvider>(
        RootSettingsPath,
        "Appearance.AvatarProvider",
        defaultValue: GitCommands.AvatarProvider.None,
        read: v => v switch { "AuthorInitials" => GitCommands.AvatarProvider.None, "Gravatar" => GitCommands.AvatarProvider.Default, _ => Enum.TryParse(v, out AvatarProvider e) ? e : GitCommands.AvatarProvider.None },
        store: v => v.ToString());

    public static ISetting<int> AvatarCacheSize { get; } = Setting.Create(RootSettingsPath, "Appearance.AvatarCacheSize", defaultValue: 200);

    // Currently not configurable in UI (Set manually in settings file)
    // Names from here: https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.brushes?view=windowsdesktop-7.0
    // or #AARRGGBB code
    public static ISetting<string> AvatarAuthorInitialsPalette { get; } = Setting.Create(RootSettingsPath, "Appearance.AvatarAuthorInitialsPalette", defaultValue: "SlateGray,RoyalBlue,Purple,OrangeRed,Teal,OliveDrab");

    // Currently not configurable in UI (Set manually in settings file)
    public static ISetting<float> AvatarAuthorInitialsLuminanceThreshold { get; } = Setting.Create(RootSettingsPath, "AvatarAuthorInitialsLuminanceThreshold", defaultValue: 0.5f);

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

    public static ISetting<string> Translation { get; } = Setting.Create(RootSettingsPath, "translation", defaultValue: "");

    private static string? _currentTranslation;

    public static string? CurrentTranslation
    {
        get => _currentTranslation ?? Translation.Value;
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

    public static ISetting<bool> UserProfileHomeDir { get; } = Setting.Create(RootSettingsPath, "userprofilehomedir", defaultValue: false);

    public static ISetting<string> CustomHomeDir { get; } = Setting.Create(RootSettingsPath, "customhomedir", defaultValue: "");

    public static ISetting<bool> EnableAutoScale { get; } = Setting.Create(RootSettingsPath, "enableautoscale", defaultValue: true);

    public static ISetting<bool> CloseCommitDialogAfterCommit { get; } = Setting.Create(RootSettingsPath, "closecommitdialogaftercommit", defaultValue: true);

    public static ISetting<bool> CloseCommitDialogAfterLastCommit { get; } = Setting.Create(RootSettingsPath, "closecommitdialogafterlastcommit", defaultValue: true);

    public static ISetting<bool> RefreshArtificialCommitOnApplicationActivated { get; } = Setting.Create(RootSettingsPath, "refreshcommitdialogonformfocus", defaultValue: false);

    public static ISetting<bool> StageInSuperprojectAfterCommit { get; } = Setting.Create(RootSettingsPath, "stageinsuperprojectaftercommit", defaultValue: true);

    public static ISetting<bool> FollowRenamesInFileHistory { get; } = Setting.Create(RootSettingsPath, "followrenamesinfilehistory", defaultValue: true);

    public static ISetting<bool> FollowRenamesInFileHistoryExactOnly { get; } = Setting.Create(RootSettingsPath, "followrenamesinfilehistoryexactonly", defaultValue: false);

    public static ISetting<bool> FullHistoryInFileHistory { get; } = Setting.Create(RootSettingsPath, "fullhistoryinfilehistory", defaultValue: false);

    public static ISetting<bool> SimplifyMergesInFileHistory { get; } = Setting.Create(RootSettingsPath, "simplifymergesinfileHistory", defaultValue: false);

    public static ISetting<bool> LoadFileHistoryOnShow { get; } = Setting.Create(RootSettingsPath, "LoadFileHistoryOnShow", defaultValue: true);

    public static ISetting<bool> LoadBlameOnShow { get; } = Setting.Create(RootSettingsPath, "LoadBlameOnShow", defaultValue: true);

    public static ISetting<bool> DetectCopyInFileOnBlame { get; } = Setting.Create(RootSettingsPath, "DetectCopyInFileOnBlame", defaultValue: false);

    public static ISetting<bool> DetectCopyInAllOnBlame { get; } = Setting.Create(RootSettingsPath, "DetectCopyInAllOnBlame", defaultValue: false);

    public static ISetting<bool> IgnoreWhitespaceOnBlame { get; } = Setting.Create(RootSettingsPath, "IgnoreWhitespaceOnBlame", defaultValue: true);

    public static ISetting<bool> OpenSubmoduleDiffInSeparateWindow { get; } = Setting.Create(RootSettingsPath, "opensubmodulediffinseparatewindow", defaultValue: false);

    /// <summary>
    /// Gets or sets whether to show artificial commits in the revision graph.
    /// </summary>
    public static ISetting<bool> RevisionGraphShowArtificialCommits { get; } = Setting.Create(RootSettingsPath, "revisiongraphshowworkingdirchanges", defaultValue: true);

    public static ISetting<bool> RevisionGraphDrawAlternateBackColor { get; } = Setting.Create(RootSettingsPath, "RevisionGraphDrawAlternateBackColor", defaultValue: true);

    public static ISetting<bool> RevisionGraphDrawNonRelativesGray { get; } = Setting.Create(RootSettingsPath, "revisiongraphdrawnonrelativesgray", defaultValue: true);

    public static ISetting<bool> RevisionGraphDrawNonRelativesTextGray { get; } = Setting.Create(RootSettingsPath, "revisiongraphdrawnonrelativestextgray", defaultValue: false);

    public static readonly Dictionary<string, Encoding> AvailableEncodings = [];

    /// <summary>
    /// Gets or sets the default pull action that is performed by the toolbar icon when it is clicked on.
    /// </summary>
    public static ISetting<GitPullAction> DefaultPullAction { get; } = Setting.Create(RootSettingsPath, "DefaultPullAction", defaultValue: GitPullAction.Merge);

    /// <summary>
    /// Gets or sets the default pull action as configured in the FormPull dialog.
    /// </summary>
    public static ISetting<GitPullAction> FormPullAction { get; } = Setting.Create(RootSettingsPath, "FormPullAction", defaultValue: GitPullAction.Merge);

    public static ISetting<bool> AutoStash { get; } = Setting.Create(RootSettingsPath, "autostash", defaultValue: false);

    public static ISetting<bool> RebaseAutoStash { get; } = Setting.Create(RootSettingsPath, "RebaseAutostash", defaultValue: false);

    public static ISetting<LocalChangesAction> CheckoutBranchAction { get; } = Setting.Create(RootSettingsPath, "checkoutbranchaction", defaultValue: LocalChangesAction.DontChange);

    public static ISetting<bool> CheckoutOtherBranchAfterReset { get; } = Setting.Create(DialogSettingsPath, nameof(CheckoutOtherBranchAfterReset), defaultValue: true);

    public static ISetting<bool> UseDefaultCheckoutBranchAction { get; } = Setting.Create(RootSettingsPath, "UseDefaultCheckoutBranchAction", defaultValue: false);

    public static ISetting<bool> DontShowHelpImages { get; } = Setting.Create(RootSettingsPath, "DontShowHelpImages", defaultValue: false);

    public static ISetting<bool> AlwaysShowAdvOpt { get; } = Setting.Create(RootSettingsPath, "AlwaysShowAdvOpt", defaultValue: false);

    public static ISetting<bool> DontConfirmAmend { get; } = Setting.Create(RootSettingsPath, "DontConfirmAmend", defaultValue: false);

    public static ISetting<bool> DontConfirmDeleteUnmergedBranch { get; } = Setting.Create(RootSettingsPath, "DontConfirmDeleteUnmergedBranch", defaultValue: false);

    public static ISetting<bool> DontConfirmCommitIfNoBranch { get; } = Setting.Create(RootSettingsPath, "DontConfirmCommitIfNoBranch", defaultValue: false);

    public static ISetting<bool> ConfirmBranchCheckout { get; } = Setting.Create(ConfirmationsSettingsPath, nameof(ConfirmBranchCheckout), defaultValue: false);

    public static ISetting<bool?> AutoPopStashAfterPull { get; } = Setting.Create<bool>(RootSettingsPath, "AutoPopStashAfterPull");

    public static ISetting<bool?> AutoPopStashAfterCheckoutBranch { get; } = Setting.Create<bool>(RootSettingsPath, "AutoPopStashAfterCheckoutBranch");

    public static ISetting<GitPullAction?> AutoPullOnPushRejectedAction { get; } = Setting.Create<GitPullAction>(RootSettingsPath, "AutoPullOnPushRejectedAction");

    public static ISetting<bool> DontConfirmPushNewBranch { get; } = Setting.Create(RootSettingsPath, "DontConfirmPushNewBranch", defaultValue: false);

    public static ISetting<bool> DontConfirmAddTrackingRef { get; } = Setting.Create(RootSettingsPath, "DontConfirmAddTrackingRef", defaultValue: false);

    public static ISetting<bool> DontConfirmCommitAfterConflictsResolved { get; } = Setting.Create(RootSettingsPath, "DontConfirmCommitAfterConflictsResolved", defaultValue: false);

    public static ISetting<bool> DontConfirmSecondAbortConfirmation { get; } = Setting.Create(RootSettingsPath, "DontConfirmSecondAbortConfirmation", defaultValue: false);

    public static ISetting<bool> DontConfirmRebase { get; } = Setting.Create(RootSettingsPath, "DontConfirmRebase", defaultValue: false);

    public static ISetting<bool> DontConfirmResolveConflicts { get; } = Setting.Create(RootSettingsPath, "DontConfirmResolveConflicts", defaultValue: false);

    public static ISetting<bool> DontConfirmUndoLastCommit { get; } = Setting.Create(RootSettingsPath, "DontConfirmUndoLastCommit", defaultValue: false);

    public static ISetting<bool> DontConfirmFetchAndPruneAll { get; } = Setting.Create(RootSettingsPath, "DontConfirmFetchAndPruneAll", defaultValue: false);

    public static ISetting<bool> DontConfirmSwitchWorktree { get; } = Setting.Create(RootSettingsPath, "DontConfirmSwitchWorktree", defaultValue: false);

    public static ISetting<bool> IncludeUntrackedFilesInAutoStash { get; } = Setting.Create(RootSettingsPath, "includeUntrackedFilesInAutoStash", defaultValue: false);

    public static ISetting<bool> IncludeUntrackedFilesInManualStash { get; } = Setting.Create(RootSettingsPath, "includeUntrackedFilesInManualStash", defaultValue: false);

    public static ISetting<bool> ShowRemoteBranches { get; } = Setting.Create(RootSettingsPath, "showRemoteBranches", defaultValue: true);

    public static BoolRuntimeSetting ShowReflogReferences { get; } = new(RootSettingsPath, nameof(ShowReflogReferences), false);

    public static ISetting<bool> ShowStashes { get; } = Setting.Create(RootSettingsPath, "showStashes", defaultValue: true);

    // Currently not configurable in UI (Set manually in settings file)
    public static ISetting<int> MaxStashesWithUntrackedFiles { get; } = Setting.Create(RootSettingsPath, "maxStashesWithUntrackedFiles", defaultValue: 10);

    public static ISetting<bool> ShowSuperprojectTags { get; } = Setting.Create(RootSettingsPath, "showSuperprojectTags", defaultValue: false);

    public static ISetting<bool> ShowSuperprojectBranches { get; } = Setting.Create(RootSettingsPath, "showSuperprojectBranches", defaultValue: true);

    public static ISetting<bool> ShowSuperprojectRemoteBranches { get; } = Setting.Create(RootSettingsPath, "showSuperprojectRemoteBranches", defaultValue: false);

    public static ISetting<bool?> UpdateSubmodulesOnCheckout { get; } = Setting.Create<bool>(RootSettingsPath, "updateSubmodulesOnCheckout");

    public static ISetting<bool?> DontConfirmUpdateSubmodulesOnCheckout { get; } = Setting.Create<bool>(RootSettingsPath, "dontConfirmUpdateSubmodulesOnCheckout");

    public static string Dictionary
    {
        get => SettingsContainer.Detached().Dictionary;
        set => SettingsContainer.Detached().Dictionary = value;
    }

    public static ISetting<bool> ShowGitCommandLine { get; } = Setting.Create(RootSettingsPath, "showgitcommandline", defaultValue: false);

    public static ISetting<bool> ShowStashCount { get; } = Setting.Create(RootSettingsPath, "showstashcount", defaultValue: false);

    public static ISetting<bool> ShowAheadBehindData { get; } = Setting.Create(RootSettingsPath, "showaheadbehinddata", defaultValue: true);

    public static ISetting<bool> ShowSubmoduleStatus { get; } = Setting.Create(RootSettingsPath, "showsubmodulestatus", defaultValue: false);

    public static ISetting<bool> RelativeDate { get; } = Setting.Create(RootSettingsPath, "relativedate", defaultValue: true);

    public static ISetting<bool> ShowGitNotes { get; } = Setting.Create(RootSettingsPath, "showgitnotes", defaultValue: false);

    public static ISetting<bool> ShowSessionRefs { get; } = Setting.Create(RootSettingsPath, "showSessionRefs", defaultValue: false);

    public static ISetting<bool> ShowGitNotesColumn { get; } = Setting.Create(AppearanceSettingsPath, nameof(ShowGitNotesColumn), defaultValue: false);

    public static ISetting<bool> ShowAnnotatedTagsMessages { get; } = Setting.Create(RootSettingsPath, "showannotatedtagsmessages", defaultValue: true);

    // History Compatibility: The meaning of this value is changed in the GUI, setting name is kept for compatibility
    public static ISetting<bool> HideMergeCommits { get; } = Setting.CreateIntercepted<bool, bool>(
        RootSettingsPath,
        "showmergecommits",
        defaultValue: false,
        read: v => !v,
        store: v => !v);

    public static ISetting<bool> ShowTags { get; } = Setting.Create(RootSettingsPath, "showtags", defaultValue: true);

    #region Revision grid column visibilities

    public static ISetting<bool> ShowRevisionGridGraphColumn { get; } = Setting.Create(RootSettingsPath, "showrevisiongridgraphcolumn", defaultValue: true);

    public static ISetting<bool> ShowAuthorAvatarColumn { get; } = Setting.Create(RootSettingsPath, "showrevisiongridauthoravatarcolumn", defaultValue: true);

    public static ISetting<bool> ShowAuthorNameColumn { get; } = Setting.Create(RootSettingsPath, "showrevisiongridauthornamecolumn", defaultValue: true);

    public static ISetting<bool> ShowDateColumn { get; } = Setting.Create(RootSettingsPath, "showrevisiongriddatecolumn", defaultValue: true);

    public static ISetting<bool> ShowObjectIdColumn { get; } = Setting.Create(RootSettingsPath, "showids", defaultValue: true);

    public static ISetting<bool> ShowBuildStatusIconColumn { get; } = Setting.Create(RootSettingsPath, "showbuildstatusiconcolumn", defaultValue: true);

    public static ISetting<bool> ShowBuildStatusTextColumn { get; } = Setting.Create(RootSettingsPath, "showbuildstatustextcolumn", defaultValue: false);

    #endregion

    public static ISetting<bool> ShowAuthorDate { get; } = Setting.Create(RootSettingsPath, "showauthordate", defaultValue: true);

    public static ISetting<bool> CloseProcessDialog { get; } = Setting.Create(RootSettingsPath, "closeprocessdialog", defaultValue: false);

    public static ISetting<bool> ShowProcessDialogPasswordInput => Setting.Create(DetailedSettingsPath, nameof(ShowProcessDialogPasswordInput), defaultValue: false);

    public static BoolRuntimeSetting ShowCurrentBranchOnly { get; } = new(RootSettingsPath, nameof(ShowCurrentBranchOnly), false);

    public static ISetting<bool> ShowSimplifyByDecoration { get; } = Setting.Create(RootSettingsPath, "showsimplifybydecoration", defaultValue: false);

    public static BoolRuntimeSetting BranchFilterEnabled { get; } = new(RootSettingsPath, nameof(BranchFilterEnabled), false);

    public static ISetting<bool> ShowOnlyFirstParent { get; } = Setting.Create(RootSettingsPath, "showfirstparent", defaultValue: false);

    public static string[] RevisionFilterDropdowns
    {
        get => GetString("RevisionFilterDropdowns", string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        set => SetString("RevisionFilterDropdowns", string.Join("\n", value ?? []));
    }

    public static ISetting<bool> CommitDialogSelectionFilter { get; } = Setting.Create(RootSettingsPath, "commitdialogselectionfilter", defaultValue: false);

    public static ISetting<string> DefaultCloneDestinationPath { get; } = Setting.Create(RootSettingsPath, "defaultclonedestinationpath", defaultValue: "");

    public static ISetting<int> RevisionGridQuickSearchTimeout { get; } = Setting.Create(RootSettingsPath, "revisiongridquicksearchtimeout", defaultValue: 4000);

    public static ISetting<bool> ShowCommitBodyInRevisionGrid { get; } = Setting.Create(RootSettingsPath, "ShowCommitBodyInRevisionGrid", defaultValue: true);

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

    public static ISetting<int> MaxRevisionGraphCommits { get; } = Setting.Create(RootSettingsPath, "maxrevisiongraphcommits", defaultValue: 100000);

    public static ISetting<bool> ShowDiffForAllParents { get; } = Setting.Create(RootSettingsPath, "showdiffforallparents", defaultValue: true);

    public static ISetting<bool> ShowFindInCommitFilesGitGrep { get; } = Setting.Create(AppearanceSettingsPath, nameof(ShowFindInCommitFilesGitGrep), defaultValue: false);
    public static ISetting<bool> ShowRevisionGridTooltips { get; } = Setting.Create(AppearanceSettingsPath, nameof(ShowRevisionGridTooltips), defaultValue: true);

    public static ISetting<bool> ShowAvailableDiffTools { get; } = Setting.Create(RootSettingsPath, "difftools.showavailable", defaultValue: true);

    public static ISetting<int> DiffVerticalRulerPosition { get; } = Setting.Create(RootSettingsPath, "diffverticalrulerposition", defaultValue: 0);

    public static ISetting<string> GitGrepUserArguments { get; } = Setting.Create(DialogSettingsPath, nameof(GitGrepUserArguments), defaultValue: "");

    public static ISetting<bool> GitGrepIgnoreCase { get; } = Setting.Create(DialogSettingsPath, nameof(GitGrepIgnoreCase), defaultValue: false);

    public static ISetting<bool> GitGrepMatchWholeWord { get; } = Setting.Create(DialogSettingsPath, nameof(GitGrepMatchWholeWord), defaultValue: false);

    public static ISetting<string> RecentWorkingDir { get; } = Setting.Create(RootSettingsPath, "RecentWorkingDir", defaultValue: (string?)null);

    public static ISetting<bool> StartWithRecentWorkingDir { get; } = Setting.Create(RootSettingsPath, "StartWithRecentWorkingDir", defaultValue: false);

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

    public static ISetting<bool> AutoStartPageant { get; } = Setting.Create(RootSettingsPath, "autostartpageant", defaultValue: true);

    public static ISetting<bool> MarkIllFormedLinesInCommitMsg { get; } = Setting.Create(RootSettingsPath, "markillformedlinesincommitmsg", defaultValue: true);

    public static ISetting<bool> UseSystemVisualStyle { get; } = Setting.Create(RootSettingsPath, "systemvisualstyle", defaultValue: true);

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

    public static Font ConEmuConsoleFont
    {
        get => GetFont("conemuconsolefont", new Font("Consolas", 12));
        set => SetFont("conemuconsolefont", value);
    }

    public static ISetting<bool> ShowEolMarkerAsGlyph { get; } = Setting.Create(RootSettingsPath, "ShowEolMarkerAsGlyph", defaultValue: false);

    #endregion

    public static ISetting<bool> MulticolorBranches { get; } = Setting.Create(RootSettingsPath, "multicolorbranches", defaultValue: true);

    public static ISetting<bool> HighlightAuthoredRevisions { get; } = Setting.Create(RootSettingsPath, "highlightauthoredrevisions", defaultValue: true);

    public static ISetting<bool> FillRefLabels { get; } = Setting.Create(RootSettingsPath, "FillRefLabels", defaultValue: false);

    public static ISetting<bool> MergeGraphLanesHavingCommonParent { get; } = Setting.Create(RevisionGraphSettingsPath, nameof(MergeGraphLanesHavingCommonParent), defaultValue: true);

    public static ISetting<bool> RenderGraphWithDiagonals { get; } = Setting.Create(RevisionGraphSettingsPath, nameof(RenderGraphWithDiagonals), defaultValue: true);

    public static ISetting<bool> StraightenGraphDiagonals { get; } = Setting.Create(RevisionGraphSettingsPath, nameof(StraightenGraphDiagonals), defaultValue: true);

    /// <summary>
    ///  The limit when to skip the straightening of revision graph segments.
    /// </summary>
    /// <remarks>
    ///  Straightening needs to call the expensive RevisionGraphRow.BuildSegmentLanes function.<br></br>
    ///  Straightening inserts gaps making the graph wider. If it already has to display many segments, i.e. parallel branches, there would be a low benefit of straightening.<br></br>
    ///  So rather skip the - in this case particularly expensive - RevisionGraphRow.BuildSegmentLanes function and call it only if the row is visible.
    /// </remarks>
    public static ISetting<int> StraightenGraphSegmentsLimit { get; } = Setting.Create(RevisionGraphSettingsPath, nameof(StraightenGraphSegmentsLimit), defaultValue: 80);

    public static ISetting<string> LastFormatPatchDir { get; } = Setting.Create(RootSettingsPath, "lastformatpatchdir", defaultValue: "");

    public static EnumRuntimeSetting<IgnoreWhitespaceKind> IgnoreWhitespaceKind { get; } = new(RootSettingsPath, nameof(IgnoreWhitespaceKind), Settings.IgnoreWhitespaceKind.None);

    public static ISetting<bool> RememberIgnoreWhiteSpacePreference { get; } = Setting.Create(RootSettingsPath, "rememberIgnoreWhiteSpacePreference", defaultValue: true);

    public static BoolRuntimeSetting ShowNonPrintingChars { get; } = new(RootSettingsPath, nameof(ShowNonPrintingChars), false);

    public static ISetting<bool> RememberShowNonPrintingCharsPreference { get; } = Setting.Create(RootSettingsPath, "RememberShowNonPrintableCharsPreference", defaultValue: false);

    public static BoolRuntimeSetting ShowEntireFile { get; } = new(RootSettingsPath, nameof(ShowEntireFile), false);

    public static ISetting<bool> RememberShowEntireFilePreference { get; } = Setting.Create(RootSettingsPath, "RememberShowEntireFilePreference", defaultValue: false);

    /// <summary>
    /// Diff appearance, alternatives to "patch" viewer.
    /// </summary>
    public static EnumRuntimeSetting<DiffDisplayAppearance> DiffDisplayAppearance { get; } = new(RootSettingsPath, nameof(DiffDisplayAppearance), Settings.DiffDisplayAppearance.Patch);

    /// <summary>
    /// Gets or sets whether to remember the preference for diff appearance.
    /// </summary>
    public static ISetting<bool> RememberDiffDisplayAppearance { get; } = Setting.Create(AppearanceSettingsPath, nameof(RememberDiffDisplayAppearance), defaultValue: false);

    public static int NumberOfContextLines
    {
        get
        {
            const int defaultValue = 3;
            return RememberNumberOfContextLines.Value ? GetInt("NumberOfContextLines", defaultValue) : defaultValue;
        }
        set
        {
            if (RememberNumberOfContextLines.Value)
            {
                SetInt("NumberOfContextLines", value);
            }
        }
    }

    public static ISetting<bool> RememberNumberOfContextLines { get; } = Setting.Create(RootSettingsPath, "RememberNumberOfContextLines", defaultValue: false);

    public static BoolRuntimeSetting ShowSyntaxHighlightingInDiff { get; } = new(RootSettingsPath, nameof(ShowSyntaxHighlightingInDiff), true);

    public static ISetting<bool> RememberShowSyntaxHighlightingInDiff { get; } = Setting.Create(RootSettingsPath, "RememberShowSyntaxHighlightingInDiff", defaultValue: true);

    public static string GetDictionaryDir()
    {
        return Path.Combine(GetResourceDir()!, "Dictionaries");
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

    public static ISetting<bool> ShowRepoCurrentBranch { get; } = Setting.Create(RootSettingsPath, "dashboardshowcurrentbranch", defaultValue: true);

    public static ISetting<string> OwnScripts { get; } = Setting.Create(RootSettingsPath, "ownScripts", defaultValue: "");

    public static ISetting<int> RecursiveSubmodules { get; } = Setting.Create(RootSettingsPath, "RecursiveSubmodules", defaultValue: 1);

    public static ISetting<ShorteningRecentRepoPathStrategy> ShorteningRecentRepoPathStrategy { get; } = Setting.Create(RootSettingsPath, "ShorteningRecentRepoPathStrategy", GitCommands.ShorteningRecentRepoPathStrategy.None);

    // History Compatibility: Keep original key to maintain the compatibility with the existing user settings
    public static ISetting<int> MaxTopRepositories { get; } = Setting.Create(RootSettingsPath, "MaxMostRecentRepositories", defaultValue: 0);

    public static ISetting<int> RecentRepositoriesHistorySize { get; } = Setting.Create(RootSettingsPath, "history size", defaultValue: 30);

    public static ISetting<bool> HideTopRepositoriesFromRecentList { get; } = Setting.Create(RecentRepositories, nameof(HideTopRepositoriesFromRecentList), defaultValue: false);

    // Currently not configurable in UI (Set manually in settings file)
    public static ISetting<int> RemotesCacheLength { get; } = Setting.Create(RootSettingsPath, "RemotesCacheLength", defaultValue: 30);

    public static ISetting<int> RecentReposComboMinWidth { get; } = Setting.Create(RootSettingsPath, "RecentReposComboMinWidth", defaultValue: 0);

    public static ISetting<string> SerializedHotkeys { get; } = Setting.Create(RootSettingsPath, "SerializedHotkeys", defaultValue: (string?)null);

    public static ISetting<bool> SortTopRepos { get; } = Setting.Create(RootSettingsPath, "SortMostRecentRepos", defaultValue: false);

    public static ISetting<bool> SortRecentRepos { get; } = Setting.Create(RootSettingsPath, "SortLessRecentRepos", defaultValue: false);

    public static ISetting<bool> DontCommitMerge { get; } = Setting.Create(RootSettingsPath, "DontCommitMerge", defaultValue: false);

    public static ISetting<int> CommitValidationMaxCntCharsFirstLine { get; } = Setting.Create(RootSettingsPath, "CommitValidationMaxCntCharsFirstLine", defaultValue: 0);

    public static ISetting<int> CommitValidationMaxCntCharsPerLine { get; } = Setting.Create(RootSettingsPath, "CommitValidationMaxCntCharsPerLine", defaultValue: 0);

    public static ISetting<bool> CommitValidationSecondLineMustBeEmpty { get; } = Setting.Create(RootSettingsPath, "CommitValidationSecondLineMustBeEmpty", defaultValue: false);

    public static ISetting<bool> CommitValidationIndentAfterFirstLine { get; } = Setting.Create(RootSettingsPath, "CommitValidationIndentAfterFirstLine", defaultValue: true);

    public static ISetting<bool> CommitValidationAutoWrap { get; } = Setting.Create(RootSettingsPath, "CommitValidationAutoWrap", defaultValue: true);

    public static ISetting<string> CommitValidationRegEx { get; } = Setting.Create(RootSettingsPath, "CommitValidationRegEx", defaultValue: "");

    public static ISetting<string> CommitTemplates { get; } = Setting.Create(RootSettingsPath, "CommitTemplates", defaultValue: "");

    public static ISetting<bool> CreateLocalBranchForRemote { get; } = Setting.Create(RootSettingsPath, "CreateLocalBranchForRemote", defaultValue: false);

    public static ISetting<bool> UseFormCommitMessage { get; } = Setting.Create(RootSettingsPath, "UseFormCommitMessage", defaultValue: true);

    public static ISetting<bool> CommitAutomaticallyAfterCherryPick { get; } = Setting.Create(RootSettingsPath, "CommitAutomaticallyAfterCherryPick", defaultValue: false);

    public static ISetting<bool> AddCommitReferenceToCherryPick { get; } = Setting.Create(RootSettingsPath, "AddCommitReferenceToCherryPick", defaultValue: false);

    public static ISetting<DateTime> LastUpdateCheck { get; } = Setting.Create(RootSettingsPath, "LastUpdateCheck", defaultValue: default(DateTime));

    public static ISetting<bool> CheckForUpdates { get; } = Setting.Create(RootSettingsPath, "CheckForUpdates", defaultValue: true);

    public static ISetting<bool> CheckForReleaseCandidates { get; } = Setting.Create(RootSettingsPath, "CheckForReleaseCandidates", defaultValue: false);

    public static ISetting<bool> OmitUninterestingDiff { get; } = Setting.Create(RootSettingsPath, "OmitUninterestingDiff", defaultValue: false);

    public static ISetting<bool> UseConsoleEmulatorForCommands { get; } = Setting.Create(RootSettingsPath, "UseConsoleEmulatorForCommands", defaultValue: true);

    public static ISetting<GitRefsSortBy> RefsSortBy { get; } = Setting.Create(RootSettingsPath, "RefsSortBy", defaultValue: GitRefsSortBy.Default);

    public static ISetting<GitRefsSortOrder> RefsSortOrder { get; } = Setting.Create(RootSettingsPath, "RefsSortOrder", defaultValue: GitRefsSortOrder.Descending);

    public static ISetting<DiffListSortType> DiffListSorting { get; } = Setting.Create(RootSettingsPath, "DiffListSortType", defaultValue: DiffListSortType.FilePath);

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

    public static ISetting<bool> RepoObjectsTreeShowBranches { get; } = Setting.Create(RootSettingsPath, "RepoObjectsTree.ShowBranches", defaultValue: true);

    public static ISetting<bool> RepoObjectsTreeShowRemotes { get; } = Setting.Create(RootSettingsPath, "RepoObjectsTree.ShowRemotes", defaultValue: true);

    public static ISetting<bool> RepoObjectsTreeShowTags { get; } = Setting.Create(RootSettingsPath, "RepoObjectsTree.ShowTags", defaultValue: true);

    public static ISetting<bool> RepoObjectsTreeShowStashes { get; } = Setting.Create(RootSettingsPath, "RepoObjectsTree.ShowStashes", defaultValue: true);

    public static ISetting<bool> RepoObjectsTreeShowSubmodules { get; } = Setting.Create(RootSettingsPath, "RepoObjectsTree.ShowSubmodules", defaultValue: true);

    public static ISetting<bool> RepoObjectsTreeShowWorktrees { get; } = Setting.Create(RootSettingsPath, "RepoObjectsTree.ShowWorktrees", defaultValue: true);

    public static ISetting<int> RepoObjectsTreeBranchesIndex { get; } = Setting.Create(RootSettingsPath, "RepoObjectsTree.BranchesIndex", defaultValue: 0);

    public static ISetting<int> RepoObjectsTreeRemotesIndex { get; } = Setting.Create(RootSettingsPath, "RepoObjectsTree.RemotesIndex", defaultValue: 1);

    public static ISetting<int> RepoObjectsTreeWorktreesIndex { get; } = Setting.Create(RootSettingsPath, "RepoObjectsTree.WorktreesIndex", defaultValue: 2);

    public static ISetting<int> RepoObjectsTreeTagsIndex { get; } = Setting.Create(RootSettingsPath, "RepoObjectsTree.TagsIndex", defaultValue: 3);

    public static ISetting<int> RepoObjectsTreeSubmodulesIndex { get; } = Setting.Create(RootSettingsPath, "RepoObjectsTree.SubmodulesIndex", defaultValue: 4);

    public static ISetting<int> RepoObjectsTreeStashesIndex { get; } = Setting.Create(RootSettingsPath, "RepoObjectsTree.StashesIndex", defaultValue: 5);

    public static ISetting<string> PrioritizedBranchNames { get; } = Setting.Create(RootSettingsPath, "PrioritizedBranchNames", defaultValue: "main[^/]*|master[^/]*|release/.*");

    public static ISetting<string> PrioritizedRemoteNames { get; } = Setting.Create(RootSettingsPath, "PrioritizedRemoteNames", defaultValue: "origin|upstream");

    /// <summary>
    ///  Remote names to prefer when auto-detecting build server integration, separated by <c>|</c>.
    ///  Defaults to <c>upstream|origin</c> so that forks resolve to the upstream project's CI.
    /// </summary>
    public static ISetting<string> PrioritizedBuildServerRemoteNames { get; } = Setting.Create(RootSettingsPath, "PrioritizedBuildServerRemoteNames", defaultValue: "upstream|origin|remote");

    public static ISetting<bool> BlameDisplayAuthorFirst { get; } = Setting.Create(RootSettingsPath, "Blame.DisplayAuthorFirst", defaultValue: false);

    public static ISetting<bool> BlameShowAuthor { get; } = Setting.Create(RootSettingsPath, "Blame.ShowAuthor", defaultValue: true);

    public static ISetting<bool> BlameShowAuthorDate { get; } = Setting.Create(RootSettingsPath, "Blame.ShowAuthorDate", defaultValue: true);

    public static ISetting<bool> BlameShowAuthorTime { get; } = Setting.Create(RootSettingsPath, "Blame.ShowAuthorTime", defaultValue: true);

    public static ISetting<bool> BlameShowLineNumbers { get; } = Setting.Create(RootSettingsPath, "Blame.ShowLineNumbers", defaultValue: false);

    public static ISetting<bool> BlameShowOriginalFilePath { get; } = Setting.Create(RootSettingsPath, "Blame.ShowOriginalFilePath", defaultValue: true);

    public static ISetting<bool> BlameShowAuthorAvatar { get; } = Setting.Create(RootSettingsPath, "Blame.ShowAuthorAvatar", defaultValue: true);

    public static ISetting<bool> AutomaticContinuousScroll { get; } = Setting.Create(RootSettingsPath, "DiffViewer.AutomaticContinuousScroll", defaultValue: false);

    public static ISetting<int> AutomaticContinuousScrollDelay { get; } = Setting.Create(RootSettingsPath, "DiffViewer.AutomaticContinuousScrollDelay", defaultValue: 600);

    public static IEnumerable<string> CustomGenericRemoteNames
    {
        get => GetString("CustomGenericRemoteNames", string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries);
    }

    public static ISetting<bool> LogCaptureCallStacks { get; } = Setting.Create(RootSettingsPath, "Log.CaptureCallStacks", defaultValue: false);

    // There is a bug in .NET/.NET Designer that fails to execute Properties.Settings.Default call.
    // Return false whilst we're in the designer.
    public static bool IsPortable() => !IsDesignMode && Properties.Settings.Default.IsPortable;

    // Currently not configurable in UI (Set manually in settings file)
    public static ISetting<bool> WriteErrorLog { get; } = Setting.Create(RootSettingsPath, "WriteErrorLog", defaultValue: false);

    // Currently not configurable in UI (Set manually in settings file)
    public static ISetting<bool> WorkaroundActivateFromMinimize { get; } = Setting.Create(RootSettingsPath, "WorkaroundActivateFromMinimize", defaultValue: false);

    public static ISetting<bool> GitAsyncWhenMinimized { get; } = Setting.Create(RootSettingsPath, "GitAsyncWhenMinimized", defaultValue: false);

    public static ISetting<bool> IsEditorSettingsMigrated { get; } = Setting.Create(MigrationSettingsPath, nameof(IsEditorSettingsMigrated), defaultValue: false);

    public static ISetting<string> UninformativeRepoNameRegex { get; } = Setting.Create(DetailedSettingsPath, nameof(UninformativeRepoNameRegex), defaultValue: "app|(repo(sitory)?)");

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

    private const string ObsoleteGetSetMessage = "Use ISetting<T> via Setting.Create() instead. See existing ISetting properties in this class for examples.";

    // String
    [Obsolete(ObsoleteGetSetMessage)]
    [return: NotNullIfNotNull(nameof(defaultValue))]
    public static string? GetString(string name, string? defaultValue) => SettingsContainer.GetString(name, defaultValue);
    [Obsolete(ObsoleteGetSetMessage)]
    public static void SetString(string name, string value) => SettingsContainer.SetString(name, value);

    // Bool
    [Obsolete(ObsoleteGetSetMessage)]
    public static bool? GetBool(string name) => SettingsContainer.GetBool(name);
    [Obsolete(ObsoleteGetSetMessage)]
    public static bool GetBool(string name, bool defaultValue) => SettingsContainer.GetBool(name, defaultValue);
    [Obsolete(ObsoleteGetSetMessage)]
    public static void SetBool(string name, bool? value) => SettingsContainer.SetBool(name, value);

    // Int
    [Obsolete(ObsoleteGetSetMessage)]
    public static int? GetInt(string name) => SettingsContainer.GetInt(name);
    [Obsolete(ObsoleteGetSetMessage)]
    public static int GetInt(string name, int defaultValue) => SettingsContainer.GetInt(name, defaultValue);
    [Obsolete(ObsoleteGetSetMessage)]
    public static void SetInt(string name, int? value) => SettingsContainer.SetInt(name, value);

    // Float
    [Obsolete(ObsoleteGetSetMessage)]
    public static float GetFloat(string name, float defaultValue) => SettingsContainer.GetFloat(name, defaultValue);

    // Date
    [Obsolete(ObsoleteGetSetMessage)]
    public static DateTime? GetDate(string name) => SettingsContainer.GetDate(name);
    [Obsolete(ObsoleteGetSetMessage)]
    public static DateTime GetDate(string name, DateTime defaultValue) => SettingsContainer.GetDate(name, defaultValue);
    [Obsolete(ObsoleteGetSetMessage)]
    public static void SetDate(string name, DateTime? value) => SettingsContainer.SetDate(name, value);

    // Font
    [Obsolete(ObsoleteGetSetMessage)]
    public static Font GetFont(string name, Font defaultValue) => SettingsContainer.GetFont(name, defaultValue);
    [Obsolete(ObsoleteGetSetMessage)]
    public static void SetFont(string name, Font value) => SettingsContainer.SetFont(name, value);

    [Obsolete("AppSettings is no longer responsible for colors, ThemeModule is. Only used by ThemeMigration.")]
    public static Color GetColor(AppColor name)
    {
        return SettingsContainer.GetColor(name.ToString().ToLowerInvariant() + "color", AppColorDefaults.GetBy(name));
    }

    // Enum
    [Obsolete(ObsoleteGetSetMessage)]
    public static T GetEnum<T>(string name, T defaultValue) where T : struct, Enum => SettingsContainer.GetEnum(name, defaultValue);
    [Obsolete(ObsoleteGetSetMessage)]
    public static void SetEnum<T>(string name, T value) where T : Enum => SettingsContainer.SetEnum(name, value);

    [Obsolete(ObsoleteGetSetMessage)]
    public static T? GetNullableEnum<T>(string name) where T : struct, Enum => ((ISettingsValueGetter)SettingsContainer).GetValue<T>(name);
    [Obsolete(ObsoleteGetSetMessage)]
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
                string processName = Process.GetCurrentProcess().ProcessName.ToLowerInvariant();
                _isDesignMode = processName.Contains("devenv") || processName.Contains("designtoolsserver");
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
