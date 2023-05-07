using GitCommands.Settings;
using GitUIPluginInterfaces;
using Microsoft;

namespace GitUI.CommandsDialogs.SettingsDialog
{
    public class ConfigFileSettingsPage : SettingsPageWithHeader, IConfigFileSettingsPage
    {
        protected ConfigFileSettingsSet ConfigFileSettingsSet => CommonLogic.ConfigFileSettingsSet;
        protected ConfigFileSettings? CurrentSettings { get; private set; }

        protected override void Init(ISettingsPageHost pageHost)
        {
            base.Init(pageHost);

            CurrentSettings = CommonLogic.ConfigFileSettingsSet.EffectiveSettings;
        }

        protected override ISettingsSource GetCurrentSettings()
        {
            Validates.NotNull(CurrentSettings);
            return CurrentSettings;
        }

        public void SetEffectiveSettings()
        {
            if (ConfigFileSettingsSet.EffectiveSettings is not null)
            {
                SetCurrentSettings(ConfigFileSettingsSet.EffectiveSettings);
            }
        }

        public void SetLocalSettings()
        {
            if (ConfigFileSettingsSet.LocalSettings is not null)
            {
                SetCurrentSettings(ConfigFileSettingsSet.LocalSettings);
            }
        }

        public override void SetGlobalSettings()
        {
            if (ConfigFileSettingsSet.GlobalSettings is not null)
            {
                SetCurrentSettings(ConfigFileSettingsSet.GlobalSettings);
            }
        }

        public void SetSystemSettings()
        {
            if (ConfigFileSettingsSet.SystemSettings is not null)
            {
                SetCurrentSettings(ConfigFileSettingsSet.SystemSettings);
            }
        }

        private void SetCurrentSettings(ConfigFileSettings settings)
        {
            if (CurrentSettings is not null && !ReferenceEquals(CurrentSettings, ConfigFileSettingsSet.SystemSettings))
            {
                SaveSettings();
            }

            CurrentSettings = settings;

            LoadSettings();
        }
    }
}
