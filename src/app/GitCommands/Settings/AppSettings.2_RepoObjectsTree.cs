using GitCommands.Settings;

namespace GitCommands;

public static partial class AppSettings
{
    public static IntegerSetting RepoObjectsTreeBranchesIndex { get; } = new(RepoObjectsTreeSettingsPath, "BranchesIndex", defaultValue: 0);

    public static IntegerSetting RepoObjectsTreeRemotesIndex { get; } = new(RepoObjectsTreeSettingsPath, "RemotesIndex", defaultValue: 1);

    public static BooleanSetting RepoObjectsTreeShowBranches { get; } = new(RepoObjectsTreeSettingsPath, "ShowBranches", defaultValue: true);

    public static BooleanSetting RepoObjectsTreeShowRemotes { get; } = new(RepoObjectsTreeSettingsPath, "ShowRemotes", defaultValue: true);

    public static BooleanSetting RepoObjectsTreeShowStashes { get; } = new(RepoObjectsTreeSettingsPath, "ShowStashes", defaultValue: true);

    public static BooleanSetting RepoObjectsTreeShowSubmodules { get; } = new(RepoObjectsTreeSettingsPath, "ShowSubmodules", defaultValue: true);

    public static BooleanSetting RepoObjectsTreeShowTags { get; } = new(RepoObjectsTreeSettingsPath, "ShowTags", defaultValue: true);

    public static BooleanSetting RepoObjectsTreeShowWorktrees { get; } = new(RepoObjectsTreeSettingsPath, "ShowWorktrees", defaultValue: true);

    public static IntegerSetting RepoObjectsTreeStashesIndex { get; } = new(RepoObjectsTreeSettingsPath, "StashesIndex", defaultValue: 5);

    public static IntegerSetting RepoObjectsTreeSubmodulesIndex { get; } = new(RepoObjectsTreeSettingsPath, "SubmodulesIndex", defaultValue: 4);

    public static IntegerSetting RepoObjectsTreeTagsIndex { get; } = new(RepoObjectsTreeSettingsPath, "TagsIndex", defaultValue: 3);

    public static IntegerSetting RepoObjectsTreeWorktreesIndex { get; } = new(RepoObjectsTreeSettingsPath, "WorktreesIndex", defaultValue: 2);
}
