using GitCommands.Settings;

namespace GitCommands;

public static partial class AppSettings
{
    public static BooleanSetting AddCommitReferenceToCherryPick { get; } = new(RootSettingsPath, nameof(AddCommitReferenceToCherryPick), defaultValue: false);

    public static BooleanSetting AlwaysShowAdvOpt { get; } = new(RootSettingsPath, nameof(AlwaysShowAdvOpt), defaultValue: false);

    public static BooleanSetting AlwaysShowCheckoutBranchDlg { get; } = new(RootSettingsPath, nameof(AlwaysShowCheckoutBranchDlg), defaultValue: false);

    public static BooleanSetting ApplyPatchIgnoreWhitespace { get; } = new(RootSettingsPath, "applypatchignorewhitespace", defaultValue: false);

    public static BooleanSetting ApplyPatchSignOff { get; } = new(RootSettingsPath, "applypatchsignoff", defaultValue: true);

    public static BooleanSetting AutoNormaliseBranchName { get; } = new(RootSettingsPath, nameof(AutoNormaliseBranchName), defaultValue: true);

    public static NullableBooleanSetting AutoPopStashAfterCheckoutBranch { get; } = new(RootSettingsPath, nameof(AutoPopStashAfterCheckoutBranch));

    public static NullableBooleanSetting AutoPopStashAfterPull { get; } = new(RootSettingsPath, nameof(AutoPopStashAfterPull));

    public static BooleanSetting AutoStartPageant { get; } = new(RootSettingsPath, "autostartpageant", defaultValue: true);

    public static BooleanSetting AutoStash { get; } = new(RootSettingsPath, "autostash", defaultValue: false);

    public static IntegerSetting AvatarImageCacheDays { get; } = new(RootSettingsPath, "authorimagecachedays", defaultValue: 13);

    public static BoolRuntimeSetting BranchFilterEnabled { get; } = new(RootSettingsPath, nameof(BranchFilterEnabled), defaultValue: false);

    public static BooleanSetting CheckForReleaseCandidates { get; } = new(RootSettingsPath, nameof(CheckForReleaseCandidates), defaultValue: false);

    public static BooleanSetting CheckForUncommittedChangesInCheckoutBranch { get; } = new(RootSettingsPath, "checkforuncommittedchangesincheckoutbranch", defaultValue: true);

    public static BooleanSetting CheckForUpdates { get; } = new(RootSettingsPath, nameof(CheckForUpdates), defaultValue: true);

    public static EnumSetting<LocalChangesAction> CheckoutBranchAction { get; } = new(RootSettingsPath, "checkoutbranchaction", defaultValue: LocalChangesAction.DontChange);

    /// <summary>
    ///  Whether the submodules of a cloned repository shall be initialized, as chosen the last time.
    /// </summary>
    public static BooleanSetting CloneInitializeAllSubmodules { get; } = new(RootSettingsPath, nameof(CloneInitializeAllSubmodules), defaultValue: true);

    public static BooleanSetting CloseCommitDialogAfterCommit { get; } = new(RootSettingsPath, "closecommitdialogaftercommit", defaultValue: true);

    public static BooleanSetting CloseCommitDialogAfterLastCommit { get; } = new(RootSettingsPath, "closecommitdialogafterlastcommit", defaultValue: true);

    public static BooleanSetting CloseProcessDialog { get; } = new(RootSettingsPath, "closeprocessdialog", defaultValue: false);

    public static BooleanSetting CommitAndPushForcedWhenAmend { get; } = new(RootSettingsPath, nameof(CommitAndPushForcedWhenAmend), defaultValue: false);

    public static BooleanSetting CommitAutomaticallyAfterCherryPick { get; } = new(RootSettingsPath, nameof(CommitAutomaticallyAfterCherryPick), defaultValue: false);

    public static IntegerSetting CommitDialogNumberOfPreviousMessages { get; } = new(RootSettingsPath, "commitDialogNumberOfPreviousMessages", defaultValue: 6);

    public static BooleanSetting CommitDialogSelectionFilter { get; } = new(RootSettingsPath, "commitdialogselectionfilter", defaultValue: false);

    public static BooleanSetting CommitDialogShowOnlyMyMessages { get; } = new(RootSettingsPath, "commitDialogShowOnlyMyMessages", defaultValue: false);

    public static BooleanSetting CommitInfoShowContainedInBranchesLocal { get; } = new(RootSettingsPath, "commitinfoshowcontainedinbrancheslocal", defaultValue: true);

    public static BooleanSetting CommitInfoShowContainedInBranchesRemote { get; } = new(RootSettingsPath, "commitinfoshowcontainedinbranchesremote", defaultValue: false);

    public static BooleanSetting CommitInfoShowContainedInBranchesRemoteIfNoLocal { get; } = new(RootSettingsPath, "commitinfoshowcontainedinbranchesremoteifnolocal", defaultValue: false);

    public static BooleanSetting CommitInfoShowContainedInTags { get; } = new(RootSettingsPath, "commitinfoshowcontainedintags", defaultValue: true);

    public static BooleanSetting CommitInfoShowTagThisCommitDerivesFrom { get; } = new(RootSettingsPath, "commitinfoshowtagthiscommitderivesfrom", defaultValue: true);

    public static IntegerSetting CommitValidationMaxCntCharsFirstLine { get; } = new(RootSettingsPath, "CommitValidationMaxCntCharsFirstLine", defaultValue: 0);

    public static IntegerSetting CommitValidationMaxCntCharsPerLine { get; } = new(RootSettingsPath, "CommitValidationMaxCntCharsPerLine", defaultValue: 0);

    public static BooleanSetting CommitValidationAutoWrap { get; } = new(RootSettingsPath, nameof(CommitValidationAutoWrap), defaultValue: true);

    public static BooleanSetting CommitValidationIndentAfterFirstLine { get; } = new(RootSettingsPath, nameof(CommitValidationIndentAfterFirstLine), defaultValue: true);

    public static BooleanSetting CommitValidationSecondLineMustBeEmpty { get; } = new(RootSettingsPath, nameof(CommitValidationSecondLineMustBeEmpty), defaultValue: false);

    public static BooleanSetting CreateLocalBranchForRemote { get; } = new(RootSettingsPath, nameof(CreateLocalBranchForRemote), defaultValue: false);

    public static TextSetting CustomAvatarTemplate { get; } = new(RootSettingsPath, nameof(CustomAvatarTemplate), defaultValue: string.Empty);

    public static TextSetting CustomHomeDir { get; } = new(RootSettingsPath, "customhomedir", defaultValue: string.Empty);

    public static TextSetting DefaultCloneDestinationPath { get; } = new(RootSettingsPath, "defaultclonedestinationpath", defaultValue: string.Empty);

    public static BooleanSetting DetectCopyInAllOnBlame { get; } = new(RootSettingsPath, nameof(DetectCopyInAllOnBlame), defaultValue: false);

    public static BooleanSetting DetectCopyInFileOnBlame { get; } = new(RootSettingsPath, nameof(DetectCopyInFileOnBlame), defaultValue: false);

    public static EnumRuntimeSetting<DiffDisplayAppearance> DiffDisplayAppearance { get; } = new(RootSettingsPath, nameof(DiffDisplayAppearance), defaultValue: Settings.DiffDisplayAppearance.Patch);

    public static IntegerSetting DiffVerticalRulerPosition { get; } = new(RootSettingsPath, "diffverticalrulerposition", defaultValue: 0);

    public static BooleanSetting DontCommitMerge { get; } = new(RootSettingsPath, nameof(DontCommitMerge), defaultValue: false);

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

    public static BooleanSetting EnableAutoScale { get; } = new(RootSettingsPath, "enableautoscale", defaultValue: true);

    public static BooleanSetting EnsureCommitMessageSecondLineEmpty { get; } = new(RootSettingsPath, "addnewlinetocommitmessagewhenmissing", defaultValue: true);

    public static BooleanSetting FillRefLabels { get; } = new(RootSettingsPath, nameof(FillRefLabels), defaultValue: false);

    public static BooleanSetting FollowRenamesInFileHistory { get; } = new(RootSettingsPath, "followrenamesinfilehistory", defaultValue: true);

    public static BooleanSetting FollowRenamesInFileHistoryExactOnly { get; } = new(RootSettingsPath, "followrenamesinfilehistoryexactonly", defaultValue: false);

    public static BooleanSetting FullHistoryInFileHistory { get; } = new(RootSettingsPath, "fullhistoryinfilehistory", defaultValue: false);

    public static EnumRuntimeSetting<IgnoreWhitespaceKind> IgnoreWhitespaceKind { get; } = new(RootSettingsPath, nameof(IgnoreWhitespaceKind), defaultValue: Settings.IgnoreWhitespaceKind.None);

    public static BooleanSetting IgnoreWhitespaceOnBlame { get; } = new(RootSettingsPath, nameof(IgnoreWhitespaceOnBlame), defaultValue: true);

    public static BooleanSetting IncludeUntrackedFilesInAutoStash { get; } = new(RootSettingsPath, "includeUntrackedFilesInAutoStash", defaultValue: false);

    public static BooleanSetting HighlightAuthoredRevisions { get; } = new(RootSettingsPath, "highlightauthoredrevisions", defaultValue: true);

    public static BooleanSetting IncludeUntrackedFilesInManualStash { get; } = new(RootSettingsPath, "includeUntrackedFilesInManualStash", defaultValue: false);

    public static TextSetting LastCommitMessage { get; } = new(RootSettingsPath, "lastCommitMessage", defaultValue: string.Empty);

    public static TextSetting LastFormatPatchDir { get; } = new(RootSettingsPath, "lastformatpatchdir", defaultValue: string.Empty);

    public static BooleanSetting LoadBlameOnShow { get; } = new(RootSettingsPath, nameof(LoadBlameOnShow), defaultValue: true);

    public static BooleanSetting LoadFileHistoryOnShow { get; } = new(RootSettingsPath, nameof(LoadFileHistoryOnShow), defaultValue: true);

    public static BooleanSetting MarkIllFormedLinesInCommitMsg { get; } = new(RootSettingsPath, "markillformedlinesincommitmsg", defaultValue: true);

    // Currently not configurable in UI (Set manually in settings file)
    public static IntegerSetting MaxStashesWithUntrackedFiles { get; } = new(RootSettingsPath, "maxStashesWithUntrackedFiles", defaultValue: 10);

    public static IntegerSetting MaxRevisionGraphCommits { get; } = new(RootSettingsPath, "maxrevisiongraphcommits", defaultValue: 100000);

    // History Compatibility: The meaning of this value is changed in the GUI, setting name is kept for compatibility
    public static IntegerSetting MaxTopRepositories { get; } = new(RootSettingsPath, "MaxMostRecentRepositories", defaultValue: 0);

    public static BooleanSetting MulticolorBranches { get; } = new(RootSettingsPath, "multicolorbranches", defaultValue: true);

    public static BooleanSetting OmitUninterestingDiff { get; } = new(RootSettingsPath, nameof(OmitUninterestingDiff), defaultValue: false);

    public static BooleanSetting OpenSubmoduleDiffInSeparateWindow { get; } = new(RootSettingsPath, "opensubmodulediffinseparatewindow", defaultValue: false);

    public static BooleanSetting ProvideAutocompletion { get; } = new(RootSettingsPath, "provideautocompletion", defaultValue: true);

    public static BooleanSetting RebaseAutoStash { get; } = new(RootSettingsPath, "RebaseAutostash", defaultValue: false);

    public static IntegerSetting RecentReposComboMinWidth { get; } = new(RootSettingsPath, nameof(RecentReposComboMinWidth), defaultValue: 0);

    public static IntegerSetting RecentRepositoriesHistorySize { get; } = new(RootSettingsPath, "history size", defaultValue: 30);

    public static IntegerSetting RecursiveSubmodules { get; } = new(RootSettingsPath, nameof(RecursiveSubmodules), defaultValue: 1);

    public static BooleanSetting RefreshArtificialCommitOnApplicationActivated { get; } = new(RootSettingsPath, "refreshcommitdialogonformfocus", defaultValue: false);

    public static BooleanSetting RelativeDate { get; } = new(RootSettingsPath, "relativedate", defaultValue: true);

    public static BooleanSetting RememberAmendCommitState { get; } = new(RootSettingsPath, nameof(RememberAmendCommitState), defaultValue: true);

    public static BooleanSetting RememberIgnoreWhiteSpacePreference { get; } = new(RootSettingsPath, "rememberIgnoreWhiteSpacePreference", defaultValue: true);

    public static BooleanSetting RememberNumberOfContextLines { get; } = new(RootSettingsPath, nameof(RememberNumberOfContextLines), defaultValue: false);

    public static BooleanSetting RememberShowEntireFilePreference { get; } = new(RootSettingsPath, "RememberShowEntireFilePreference", defaultValue: false);

    public static BooleanSetting RememberShowNonPrintingCharsPreference { get; } = new(RootSettingsPath, "RememberShowNonPrintableCharsPreference", defaultValue: false);

    public static BooleanSetting RememberShowSyntaxHighlightingInDiff { get; } = new(RootSettingsPath, nameof(RememberShowSyntaxHighlightingInDiff), defaultValue: true);

    // Currently not configurable in UI (Set manually in settings file)
    public static IntegerSetting RemotesCacheLength { get; } = new(RootSettingsPath, nameof(RemotesCacheLength), defaultValue: 30);

    public static BooleanSetting RevisionGraphDrawAlternateBackColor { get; } = new(RootSettingsPath, nameof(RevisionGraphDrawAlternateBackColor), defaultValue: true);

    public static BooleanSetting RevisionGraphDrawNonRelativesGray { get; } = new(RootSettingsPath, "revisiongraphdrawnonrelativesgray", defaultValue: true);

    public static BooleanSetting RevisionGraphDrawNonRelativesTextGray { get; } = new(RootSettingsPath, "revisiongraphdrawnonrelativestextgray", defaultValue: false);

    /// <summary>
    ///  Gets or sets whether to show artificial commits in the revision graph.
    /// </summary>
    public static BooleanSetting RevisionGraphShowArtificialCommits { get; } = new(RootSettingsPath, "revisiongraphshowworkingdirchanges", defaultValue: true);

    public static IntegerSetting RevisionGridQuickSearchTimeout { get; } = new(RootSettingsPath, "revisiongridquicksearchtimeout", defaultValue: 4000);

    public static EnumRuntimeSetting<RevisionSortOrder> RevisionSortOrder { get; } = new(RootSettingsPath, nameof(RevisionSortOrder), defaultValue: GitCommands.RevisionSortOrder.GitDefault);

    public static BooleanSetting ShowAheadBehindData { get; } = new(RootSettingsPath, "showaheadbehinddata", defaultValue: true);

    public static BooleanSetting ShowAnnotatedTagsMessages { get; } = new(RootSettingsPath, "showannotatedtagsmessages", defaultValue: true);

    public static BooleanSetting ShowAuthorAvatarColumn { get; } = new(RootSettingsPath, "showrevisiongridauthoravatarcolumn", defaultValue: true);

    public static BooleanSetting ShowAuthorAvatarInCommitInfo { get; } = new(RootSettingsPath, "showauthorgravatar", defaultValue: true);

    public static BooleanSetting ShowAuthorDate { get; } = new(RootSettingsPath, "showauthordate", defaultValue: true);

    public static BooleanSetting ShowAuthorNameColumn { get; } = new(RootSettingsPath, "showrevisiongridauthornamecolumn", defaultValue: true);

    public static BooleanSetting ShowAvailableDiffTools { get; } = new(RootSettingsPath, "difftools.showavailable", defaultValue: true);

    public static BooleanSetting ShowBuildStatusIconColumn { get; } = new(RootSettingsPath, "showbuildstatusiconcolumn", defaultValue: true);

    public static BooleanSetting ShowBuildStatusTextColumn { get; } = new(RootSettingsPath, "showbuildstatustextcolumn", defaultValue: false);

    public static BooleanSetting ShowCommitAndPush { get; } = new(RootSettingsPath, "showcommitandpush", defaultValue: true);

    public static BooleanSetting ShowCommitBodyInRevisionGrid { get; } = new(RootSettingsPath, nameof(ShowCommitBodyInRevisionGrid), defaultValue: true);

    public static BoolRuntimeSetting ShowCurrentBranchOnly { get; } = new(RootSettingsPath, nameof(ShowCurrentBranchOnly), defaultValue: false);

    public static BooleanSetting ShowDateColumn { get; } = new(RootSettingsPath, "showrevisiongriddatecolumn", defaultValue: true);

    public static BooleanSetting ShowDiffForAllParents { get; } = new(RootSettingsPath, "showdiffforallparents", defaultValue: true);

    public static BoolRuntimeSetting ShowEntireFile { get; } = new(RootSettingsPath, nameof(ShowEntireFile), defaultValue: false);

    public static BooleanSetting ShowEolMarkerAsGlyph { get; } = new(RootSettingsPath, nameof(ShowEolMarkerAsGlyph), defaultValue: false);

    public static BooleanSetting ShowErrorsWhenStagingFiles { get; } = new(RootSettingsPath, "showerrorswhenstagingfiles", defaultValue: true);

    public static BooleanSetting ShowGitCommandLine { get; } = new(RootSettingsPath, "showgitcommandline", defaultValue: false);

    public static BooleanSetting ShowGitNotes { get; } = new(RootSettingsPath, "showgitnotes", defaultValue: false);

    public static BooleanSetting ShowGitStatusForArtificialCommits { get; } = new(RootSettingsPath, "showgitstatusforartificialcommits", defaultValue: true);

    public static BooleanSetting ShowGitStatusInBrowseToolbar { get; } = new(RootSettingsPath, "showgitstatusinbrowsetoolbar", defaultValue: true);

    public static BoolRuntimeSetting ShowNonPrintingChars { get; } = new(RootSettingsPath, nameof(ShowNonPrintingChars), defaultValue: false);

    public static BooleanSetting ShowObjectIdColumn { get; } = new(RootSettingsPath, "showids", defaultValue: true);

    public static BooleanSetting ShowOnlyFirstParent { get; } = new(RootSettingsPath, "showfirstparent", defaultValue: false);

    public static BoolRuntimeSetting ShowReflogReferences { get; } = new(RootSettingsPath, nameof(ShowReflogReferences), defaultValue: false);

    public static BooleanSetting ShowRemoteBranches { get; } = new(RootSettingsPath, "showRemoteBranches", defaultValue: true);

    public static BooleanSetting ShowRepoCurrentBranch { get; } = new(RootSettingsPath, "dashboardshowcurrentbranch", defaultValue: true);

    public static BooleanSetting ShowResetAllChanges { get; } = new(RootSettingsPath, "showresetallchanges", defaultValue: true);

    public static BooleanSetting ShowResetWorkTreeChanges { get; } = new(RootSettingsPath, "showresetunstagedchanges", defaultValue: true);

    public static BooleanSetting ShowRevisionGridGraphColumn { get; } = new(RootSettingsPath, "showrevisiongridgraphcolumn", defaultValue: true);

    public static BooleanSetting ShowSessionRefs { get; } = new(RootSettingsPath, "showSessionRefs", defaultValue: false);

    public static BooleanSetting ShowSimplifyByDecoration { get; } = new(RootSettingsPath, "showsimplifybydecoration", defaultValue: false);

    public static BooleanSetting ShowStashCount { get; } = new(RootSettingsPath, "showstashcount", defaultValue: false);

    public static BooleanSetting ShowStashes { get; } = new(RootSettingsPath, "showStashes", defaultValue: true);

    public static BooleanSetting ShowSubmoduleStatus { get; } = new(RootSettingsPath, "showsubmodulestatus", defaultValue: false);

    public static BooleanSetting ShowSuperprojectBranches { get; } = new(RootSettingsPath, "showSuperprojectBranches", defaultValue: true);

    public static BooleanSetting ShowSuperprojectRemoteBranches { get; } = new(RootSettingsPath, "showSuperprojectRemoteBranches", defaultValue: false);

    public static BooleanSetting ShowSuperprojectTags { get; } = new(RootSettingsPath, "showSuperprojectTags", defaultValue: false);

    public static BoolRuntimeSetting ShowSyntaxHighlightingInDiff { get; } = new(RootSettingsPath, nameof(ShowSyntaxHighlightingInDiff), defaultValue: true);

    public static BooleanSetting ShowTags { get; } = new(RootSettingsPath, "showtags", defaultValue: true);

    public static BooleanSetting SimplifyMergesInFileHistory { get; } = new(RootSettingsPath, "simplifymergesinfileHistory", defaultValue: false);

    public static BooleanSetting SortRecentRepos { get; } = new(RootSettingsPath, "SortLessRecentRepos", defaultValue: false);

    public static BooleanSetting SortTopRepos { get; } = new(RootSettingsPath, "SortMostRecentRepos", defaultValue: false);

    public static BooleanSetting StageInSuperprojectAfterCommit { get; } = new(RootSettingsPath, "stageinsuperprojectaftercommit", defaultValue: true);

    public static BooleanSetting StartWithRecentWorkingDir { get; } = new(RootSettingsPath, "StartWithRecentWorkingDir", defaultValue: false);

    public static BooleanSetting StashKeepIndex { get; } = new(RootSettingsPath, "stashkeepindex", defaultValue: false);

    public static NullableBooleanSetting TelemetryEnabled { get; } = new(RootSettingsPath, nameof(TelemetryEnabled));

    public static TextSetting Translation { get; } = new(RootSettingsPath, "translation", defaultValue: string.Empty);

    public static EnumSetting<TruncatePathMethod> TruncatePathMethod { get; } = new(RootSettingsPath, "truncatepathmethod", defaultValue: GitCommands.TruncatePathMethod.None);

    public static NullableBooleanSetting UpdateSubmodulesOnCheckout { get; } = new(RootSettingsPath, "updateSubmodulesOnCheckout");

    public static BooleanSetting UseConsoleEmulatorForCommands { get; } = new(RootSettingsPath, nameof(UseConsoleEmulatorForCommands), defaultValue: true);

    public static BooleanSetting UseDefaultCheckoutBranchAction { get; } = new(RootSettingsPath, nameof(UseDefaultCheckoutBranchAction), defaultValue: false);

    public static BooleanSetting UseFormCommitMessage { get; } = new(RootSettingsPath, nameof(UseFormCommitMessage), defaultValue: true);

    // History Compatibility: The settings key has patience in the name for historical reasons
    public static BooleanSetting UseHistogramDiffAlgorithm { get; } = new(RootSettingsPath, "usepatiencediffalgorithm", defaultValue: false);

    public static BooleanSetting UseSystemVisualStyle { get; } = new(RootSettingsPath, "systemvisualstyle", defaultValue: true);

    public static BooleanSetting UserProfileHomeDir { get; } = new(RootSettingsPath, "userprofilehomedir", defaultValue: false);

    // Currently not configurable in UI (Set manually in settings file)
    public static TextSetting WslCommand { get; } = new(RootSettingsPath, nameof(WslCommand), defaultValue: "wsl");

    // Currently not configurable in UI (Set manually in settings file)
    public static TextSetting WslGitCommand { get; } = new(RootSettingsPath, nameof(WslGitCommand), defaultValue: "git");

    // Currently not configurable in UI (Set manually in settings file)
    public static BooleanSetting WslGitEnabled { get; } = new(RootSettingsPath, nameof(WslGitEnabled), defaultValue: true);
}
