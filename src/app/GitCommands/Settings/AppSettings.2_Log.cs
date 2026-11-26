using GitCommands.Settings;

namespace GitCommands;

public static partial class AppSettings
{
    public static BooleanSetting LogCaptureCallStacks { get; } = new(LogSettingsPath, "CaptureCallStacks", defaultValue: false);
}
