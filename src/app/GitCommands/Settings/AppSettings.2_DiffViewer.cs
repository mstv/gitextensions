using GitCommands.Settings;

namespace GitCommands;

public static partial class AppSettings
{
    public static BooleanSetting AutomaticContinuousScroll { get; } = new(DiffViewerSettingsPath, nameof(AutomaticContinuousScroll), defaultValue: false);

    public static IntegerSetting AutomaticContinuousScrollDelay { get; } = new(DiffViewerSettingsPath, nameof(AutomaticContinuousScrollDelay), defaultValue: 600);
}
