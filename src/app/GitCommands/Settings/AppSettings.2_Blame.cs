using GitCommands.Settings;

namespace GitCommands;

public static partial class AppSettings
{
    public static BooleanSetting BlameDisplayAuthorFirst { get; } = new(BlameSettingsPath, "DisplayAuthorFirst", defaultValue: false);

    public static BooleanSetting BlameShowAuthor { get; } = new(BlameSettingsPath, "ShowAuthor", defaultValue: true);

    public static BooleanSetting BlameShowAuthorAvatar { get; } = new(BlameSettingsPath, "ShowAuthorAvatar", defaultValue: true);

    public static BooleanSetting BlameShowAuthorDate { get; } = new(BlameSettingsPath, "ShowAuthorDate", defaultValue: true);

    public static BooleanSetting BlameShowAuthorTime { get; } = new(BlameSettingsPath, "ShowAuthorTime", defaultValue: true);

    public static BooleanSetting BlameShowLineNumbers { get; } = new(BlameSettingsPath, "ShowLineNumbers", defaultValue: false);

    public static BooleanSetting BlameShowOriginalFilePath { get; } = new(BlameSettingsPath, "ShowOriginalFilePath", defaultValue: true);
}
