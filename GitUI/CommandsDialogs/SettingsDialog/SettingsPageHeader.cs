namespace GitUI.CommandsDialogs.SettingsDialog
{
    public interface IGlobalSettingsPage : ISettingsPage
    {
        void SetGlobalSettings();
    }

    public interface ILocalSettingsPage : IGlobalSettingsPage
    {
        void SetLocalSettings();

        void SetEffectiveSettings();
    }

    public interface IDistributedSettingsPage : ILocalSettingsPage
    {
        void SetDistributedSettings();
    }

    public interface IConfigFileSettingsPage : ILocalSettingsPage
    {
        void SetSystemSettings();
    }

    public partial class SettingsPageHeader
    {
        private readonly SettingsPageWithHeader? _page;

        public SettingsPageHeader(SettingsPageWithHeader? page)
        {
            InitializeComponent();
            InitializeComplete();

            label1.Font = new System.Drawing.Font(label1.Font, System.Drawing.FontStyle.Bold);

            if (page is not null)
            {
                settingsPagePanel.Controls.Add(page);
                page.Dock = DockStyle.Fill;
                _page = page;
                ConfigureHeader();
            }
        }

        private void ConfigureHeader()
        {
            if (_page is not ILocalSettingsPage localSettingsPage)
            {
                GlobalRB.Checked = true;

                EffectiveRB.Visible = false;
                arrowLocal.Visible = false;
                LocalRB.Visible = false;
                arrowDistributed.Visible = false;
                DistributedRB.Visible = false;
                arrowGlobal.Visible = false;
                arrowSystem.Visible = false;
                SystemRB.Visible = false;
                tableLayoutPanel2.RowStyles[2].Height = 0;
                return;
            }

            LocalRB.CheckedChanged += (s, e) =>
            {
                if (LocalRB.Checked)
                {
                    localSettingsPage.SetLocalSettings();
                }
            };

            EffectiveRB.CheckedChanged += (s, e) =>
            {
                if (EffectiveRB.Checked)
                {
                    arrowLocal.ForeColor = EffectiveRB.ForeColor;
                    localSettingsPage.SetEffectiveSettings();
                }
                else
                {
                    arrowLocal.ForeColor = arrowLocal.BackColor;
                }

                arrowDistributed.ForeColor = arrowLocal.ForeColor;
                arrowGlobal.ForeColor = arrowLocal.ForeColor;
                arrowSystem.ForeColor = arrowLocal.ForeColor;
            };

            EffectiveRB.Checked = true;

            if (localSettingsPage is not IDistributedSettingsPage distributedSettingsPage)
            {
                DistributedRB.Visible = false;
                arrowDistributed.Visible = false;
            }
            else
            {
                DistributedRB.CheckedChanged += (s, e) =>
                {
                    if (DistributedRB.Checked)
                    {
                        distributedSettingsPage.SetDistributedSettings();
                    }
                };
            }

            if (localSettingsPage is not IConfigFileSettingsPage configFileSettingsPage)
            {
                SystemRB.Visible = false;
                arrowSystem.Visible = false;
            }
            else
            {
                SystemRB.CheckedChanged += (s, e) =>
                {
                    if (SystemRB.Checked)
                    {
                        configFileSettingsPage.SetSystemSettings();
                    }
                };
            }
        }

        private void GlobalRB_CheckedChanged(object sender, EventArgs e)
        {
            if (GlobalRB.Checked)
            {
                _page?.SetGlobalSettings();
            }
        }
    }
}
