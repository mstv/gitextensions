using GitCommands.Settings;

namespace GitUI.CommandsDialogs.SettingsDialog
{
    public class RepoDistSettingsSet
    {
        public readonly DistributedSettings EffectiveSettings;
        public readonly DistributedSettings LocalSettings;
        public readonly DistributedSettings RepoDistSettings;
        public readonly DistributedSettings GlobalSettings;

        public RepoDistSettingsSet(
            DistributedSettings effectiveSettings,
            DistributedSettings localSettings,
            DistributedSettings pulledSettings,
            DistributedSettings globalSettings)
        {
            EffectiveSettings = effectiveSettings;
            LocalSettings = localSettings;
            RepoDistSettings = pulledSettings;
            GlobalSettings = globalSettings;
        }
    }
}
