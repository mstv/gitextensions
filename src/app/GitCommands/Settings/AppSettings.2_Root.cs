using GitCommands.Settings;

namespace GitCommands;

public static partial class AppSettings
{
    public static BooleanSetting AlwaysShowAdvOpt { get; } = new(RootSettingsPath, nameof(AlwaysShowAdvOpt), defaultValue: false);

    public static BooleanSetting ApplyPatchIgnoreWhitespace { get; } = new(RootSettingsPath, "applypatchignorewhitespace", defaultValue: false);

    public static BooleanSetting ApplyPatchSignOff { get; } = new(RootSettingsPath, "applypatchsignoff", defaultValue: true);

    public static BooleanSetting AutoNormaliseBranchName { get; } = new(RootSettingsPath, nameof(AutoNormaliseBranchName), defaultValue: true);

    public static NullableBooleanSetting AutoPopStashAfterCheckoutBranch { get; } = new(RootSettingsPath, nameof(AutoPopStashAfterCheckoutBranch));

    public static NullableBooleanSetting AutoPopStashAfterPull { get; } = new(RootSettingsPath, nameof(AutoPopStashAfterPull));

    public static BooleanSetting AutoStash { get; } = new(RootSettingsPath, "autostash", defaultValue: false);

    public static IntegerSetting AvatarCacheSize { get; } = new(RootSettingsPath, "Appearance.AvatarCacheSize", defaultValue: 200);

    public static IntegerSetting AvatarImageCacheDays { get; } = new(RootSettingsPath, "authorimagecachedays", defaultValue: 13);

    public static BoolRuntimeSetting BranchFilterEnabled { get; } = new(RootSettingsPath, nameof(BranchFilterEnabled), defaultValue: false);

    public static EnumSetting<LocalChangesAction> CheckoutBranchAction { get; } = new(RootSettingsPath, "checkoutbranchaction", defaultValue: LocalChangesAction.DontChange);

    /// <summary>
    ///  Whether the submodules of a cloned repository shall be initialized, as chosen the last time.
    /// </summary>
    public static BooleanSetting CloneInitializeAllSubmodules { get; } = new(RootSettingsPath, nameof(CloneInitializeAllSubmodules), defaultValue: true);

    public static IntegerSetting CommitDialogNumberOfPreviousMessages { get; } = new(RootSettingsPath, "commitDialogNumberOfPreviousMessages", defaultValue: 6);

    public static BooleanSetting CommitDialogShowOnlyMyMessages { get; } = new(RootSettingsPath, "commitDialogShowOnlyMyMessages", defaultValue: false);

    public static EnumRuntimeSetting<DiffDisplayAppearance> DiffDisplayAppearance { get; } = new(RootSettingsPath, nameof(DiffDisplayAppearance), defaultValue: Settings.DiffDisplayAppearance.Patch);

    public static BooleanSetting DontConfirmAddTrackingRef { get; } = new(RootSettingsPath, nameof(DontConfirmAddTrackingRef), defaultValue: false);

    public static BooleanSetting DontConfirmAmend { get; } = new(RootSettingsPath, nameof(DontConfirmAmend), defaultValue: false);

    public static BooleanSetting DontConfirmCommitAfterConflictsResolved { get; } = new(RootSettingsPath, nameof(DontConfirmCommitAfterConflictsResolved), defaultValue: false);

    public static BooleanSetting DontConfirmCommitIfNoBranch { get; } = new(RootSettingsPath, nameof(DontConfirmCommitIfNoBranch), defaultValue: false);

    public static BooleanSetting DontConfirmDeleteUnmergedBranch { get; } = new(RootSettingsPath, nameof(DontConfirmDeleteUnmergedBranch), defaultValue: false);

    public static BooleanSetting DontConfirmFetchAndPruneAll { get; } = new(RootSettingsPath, nameof(DontConfirmFetchAndPruneAll), defaultValue: false);

    public static BooleanSetting DontConfirmPushNewBranch { get; } = new(RootSettingsPath, nameof(DontConfirmPushNewBranch), defaultValue: false);

    public static BooleanSetting DontConfirmRebase { get; } = new(RootSettingsPath, nameof(DontConfirmRebase), defaultValue: false);

    public static BooleanSetting DontConfirmResolveConflicts { get; } = new(RootSettingsPath, nameof(DontConfirmResolveConflicts), defaultValue: false);

    public static BooleanSetting DontConfirmSecondAbortConfirmation { get; } = new(RootSettingsPath, nameof(DontConfirmSecondAbortConfirmation), defaultValue: false);

    // History: The setting had originally been called 'StashConfirmDropShow', and then it was inverted.
    // To maintain the compatibility with the existing user settings, negate the retrieved value.
    public static InvertedBooleanSetting DontConfirmStashDrop { get; } = new(RootSettingsPath, "stashconfirmdropshow", defaultValue: false);

    public static BooleanSetting DontConfirmSwitchWorktree { get; } = new(RootSettingsPath, nameof(DontConfirmSwitchWorktree), defaultValue: false);

    public static BooleanSetting DontConfirmUndoLastCommit { get; } = new(RootSettingsPath, nameof(DontConfirmUndoLastCommit), defaultValue: false);

    public static NullableBooleanSetting DontConfirmUpdateSubmodulesOnCheckout { get; } = new(RootSettingsPath, "dontConfirmUpdateSubmodulesOnCheckout");

    public static BooleanSetting DontShowHelpImages { get; } = new(RootSettingsPath, nameof(DontShowHelpImages), defaultValue: false);

    public static BooleanSetting EnsureCommitMessageSecondLineEmpty { get; } = new(RootSettingsPath, "addnewlinetocommitmessagewhenmissing", defaultValue: true);

    public static EnumRuntimeSetting<IgnoreWhitespaceKind> IgnoreWhitespaceKind { get; } = new(RootSettingsPath, nameof(IgnoreWhitespaceKind), defaultValue: Settings.IgnoreWhitespaceKind.None);

    public static BooleanSetting IncludeUntrackedFilesInAutoStash { get; } = new(RootSettingsPath, "includeUntrackedFilesInAutoStash", defaultValue: false);

    public static BooleanSetting IncludeUntrackedFilesInManualStash { get; } = new(RootSettingsPath, "includeUntrackedFilesInManualStash", defaultValue: false);

    public static TextSetting LastCommitMessage { get; } = new(RootSettingsPath, "lastCommitMessage", defaultValue: string.Empty);

    public static BooleanSetting ProvideAutocompletion { get; } = new(RootSettingsPath, "provideautocompletion", defaultValue: true);

    public static BooleanSetting RebaseAutoStash { get; } = new(RootSettingsPath, "RebaseAutostash", defaultValue: false);

    public static BooleanSetting RelativeDate { get; } = new(RootSettingsPath, "relativedate", defaultValue: true);

    public static BooleanSetting RememberAmendCommitState { get; } = new(RootSettingsPath, nameof(RememberAmendCommitState), defaultValue: true);

    public static EnumRuntimeSetting<RevisionSortOrder> RevisionSortOrder { get; } = new(RootSettingsPath, nameof(RevisionSortOrder), defaultValue: GitCommands.RevisionSortOrder.GitDefault);

    public static BooleanSetting ShowAheadBehindData { get; } = new(RootSettingsPath, "showaheadbehinddata", defaultValue: true);

    public static BooleanSetting ShowAnnotatedTagsMessages { get; } = new(RootSettingsPath, "showannotatedtagsmessages", defaultValue: true);

    public static BooleanSetting ShowAuthorAvatarColumn { get; } = new(RootSettingsPath, "showrevisiongridauthoravatarcolumn", defaultValue: true);

    public static BooleanSetting ShowAuthorAvatarInCommitInfo { get; } = new(RootSettingsPath, "showauthorgravatar", defaultValue: true);

    public static BooleanSetting ShowAuthorNameColumn { get; } = new(RootSettingsPath, "showrevisiongridauthornamecolumn", defaultValue: true);

    public static BooleanSetting ShowBuildStatusIconColumn { get; } = new(RootSettingsPath, "showbuildstatusiconcolumn", defaultValue: true);

    public static BooleanSetting ShowBuildStatusTextColumn { get; } = new(RootSettingsPath, "showbuildstatustextcolumn", defaultValue: false);

    public static BooleanSetting ShowCommitAndPush { get; } = new(RootSettingsPath, "showcommitandpush", defaultValue: true);

    public static BoolRuntimeSetting ShowCurrentBranchOnly { get; } = new(RootSettingsPath, nameof(ShowCurrentBranchOnly), defaultValue: false);

    public static BooleanSetting ShowDateColumn { get; } = new(RootSettingsPath, "showrevisiongriddatecolumn", defaultValue: true);

    public static BoolRuntimeSetting ShowEntireFile { get; } = new(RootSettingsPath, nameof(ShowEntireFile), defaultValue: false);

    public static BooleanSetting ShowErrorsWhenStagingFiles { get; } = new(RootSettingsPath, "showerrorswhenstagingfiles", defaultValue: true);

    public static BooleanSetting ShowGitCommandLine { get; } = new(RootSettingsPath, "showgitcommandline", defaultValue: false);

    public static BooleanSetting ShowGitNotes { get; } = new(RootSettingsPath, "showgitnotes", defaultValue: false);

    public static BooleanSetting ShowGitStatusInBrowseToolbar { get; } = new(RootSettingsPath, "showgitstatusinbrowsetoolbar", defaultValue: true);

    public static BoolRuntimeSetting ShowNonPrintingChars { get; } = new(RootSettingsPath, nameof(ShowNonPrintingChars), defaultValue: false);

    public static BooleanSetting ShowObjectIdColumn { get; } = new(RootSettingsPath, "showids", defaultValue: true);

    public static BoolRuntimeSetting ShowReflogReferences { get; } = new(RootSettingsPath, nameof(ShowReflogReferences), defaultValue: false);

    public static BooleanSetting ShowRemoteBranches { get; } = new(RootSettingsPath, "showRemoteBranches", defaultValue: true);

    public static BooleanSetting ShowResetAllChanges { get; } = new(RootSettingsPath, "showresetallchanges", defaultValue: true);

    public static BooleanSetting ShowResetWorkTreeChanges { get; } = new(RootSettingsPath, "showresetunstagedchanges", defaultValue: true);

    public static BooleanSetting ShowRevisionGridGraphColumn { get; } = new(RootSettingsPath, "showrevisiongridgraphcolumn", defaultValue: true);

    public static BooleanSetting ShowSessionRefs { get; } = new(RootSettingsPath, "showSessionRefs", defaultValue: false);

    public static BooleanSetting ShowStashCount { get; } = new(RootSettingsPath, "showstashcount", defaultValue: false);

    public static BooleanSetting ShowStashes { get; } = new(RootSettingsPath, "showStashes", defaultValue: true);

    public static BooleanSetting ShowSubmoduleStatus { get; } = new(RootSettingsPath, "showsubmodulestatus", defaultValue: false);

    public static BooleanSetting ShowSuperprojectBranches { get; } = new(RootSettingsPath, "showSuperprojectBranches", defaultValue: true);

    public static BooleanSetting ShowSuperprojectRemoteBranches { get; } = new(RootSettingsPath, "showSuperprojectRemoteBranches", defaultValue: false);

    public static BooleanSetting ShowSuperprojectTags { get; } = new(RootSettingsPath, "showSuperprojectTags", defaultValue: false);

    public static BoolRuntimeSetting ShowSyntaxHighlightingInDiff { get; } = new(RootSettingsPath, nameof(ShowSyntaxHighlightingInDiff), defaultValue: true);

    public static BooleanSetting ShowTags { get; } = new(RootSettingsPath, "showtags", defaultValue: true);

    public static BooleanSetting StashKeepIndex { get; } = new(RootSettingsPath, "stashkeepindex", defaultValue: false);

    public static NullableBooleanSetting TelemetryEnabled { get; } = new(RootSettingsPath, nameof(TelemetryEnabled));

    public static EnumSetting<TruncatePathMethod> TruncatePathMethod { get; } = new(RootSettingsPath, "truncatepathmethod", defaultValue: GitCommands.TruncatePathMethod.None);

    public static NullableBooleanSetting UpdateSubmodulesOnCheckout { get; } = new(RootSettingsPath, "updateSubmodulesOnCheckout");

    public static BooleanSetting UseConsoleEmulatorForCommands { get; } = new(RootSettingsPath, nameof(UseConsoleEmulatorForCommands), defaultValue: true);

    // History Compatibility: The settings key has patience in the name for historical reasons
    public static BooleanSetting UseHistogramDiffAlgorithm { get; } = new(RootSettingsPath, "usepatiencediffalgorithm", defaultValue: false);

    // Currently not configurable in UI (Set manually in settings file)
    public static TextSetting WslCommand { get; } = new(RootSettingsPath, nameof(WslCommand), defaultValue: "wsl");

    // Currently not configurable in UI (Set manually in settings file)
    public static TextSetting WslGitCommand { get; } = new(RootSettingsPath, nameof(WslGitCommand), defaultValue: "git");

    // Currently not configurable in UI (Set manually in settings file)
    public static BooleanSetting WslGitEnabled { get; } = new(RootSettingsPath, nameof(WslGitEnabled), defaultValue: true);
}
