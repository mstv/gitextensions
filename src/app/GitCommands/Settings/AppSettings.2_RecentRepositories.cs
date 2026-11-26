using GitCommands.Settings;

namespace GitCommands;

public static partial class AppSettings
{
    public static BooleanSetting HideTopRepositoriesFromRecentList { get; } = new(RecentRepositoriesSettingsPath, nameof(HideTopRepositoriesFromRecentList), defaultValue: false);
}
