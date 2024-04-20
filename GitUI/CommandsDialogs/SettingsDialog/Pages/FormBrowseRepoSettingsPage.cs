using GitCommands;
using GitUI.Hotkey;
using GitUI.Shells;
using GitUIPluginInterfaces;
using ResourceManager;
using ResourceManager.Hotkey;

namespace GitUI.CommandsDialogs.SettingsDialog.Pages
{
    public partial class FormBrowseRepoSettingsPage : SettingsPageWithHeader
    {
        private const string _processHistoryUrl = "https://git-extensions-documentation.readthedocs.io/settings.html#process-history-as-tab-otherwise-as-panel";
        private readonly TranslationString _processHistoryTooltip
            = new("""
                  The output displayed in the process dialog and the trace output is retained and shown in the Process History.
                  Focus the Process History or toggle its visibility using the hotkey {0}.
                  With this set, the Process History is displayed in a tab in the lower pane of the Browse Repository window.
                  With this unset, the Process History is displayed in a panel docked to the lower left corner of the Browse Repository window.
                  The panel also uses some space of the file status list of the Diff tab.
                  The panel height can be adapted using the splitter control of the Left Panel.
                  The Process History can be disabled by setting the process history depth to 0.
                  """);
        private readonly ShellProvider _shellProvider = new();
        private int _cboTerminalPreviousIndex = -1;

        public FormBrowseRepoSettingsPage(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            InitializeComponent();
            cboTerminal.DisplayMember = "Name";
            InitializeComplete();
            string hotkey = serviceProvider.GetRequiredService<IHotkeySettingsManager>()
                .LoadHotkeys(FormBrowse.HotkeySettingsName)
                .GetShortcutDisplay(FormBrowse.Command.ToggleHistory);
            chkProcessHistoryAsTab.ToolTipText = string.Format(_processHistoryTooltip.Text, hotkey);
        }

        protected override void Init(ISettingsPageHost pageHost)
        {
            base.Init(pageHost);
        }

        protected override void PageToSettings()
        {
            AppSettings.ShowConEmuTab.Value = chkChowConsoleTab.Checked;
            AppSettings.UseBrowseForFileHistory.Value = chkUseBrowseForFileHistory.Checked;
            AppSettings.UseDiffViewerForBlame.Value = chkUseDiffViewerForBlame.Checked;
            AppSettings.ShowGpgInformation.Value = chkShowGpgInformation.Checked;

            int processHistoryDepth = (int)_NO_TRANSLATE_ProcessHistoryDepth.Value;
            bool changed = AppSettings.ProcessHistoryAsTab.Value != chkProcessHistoryAsTab.Checked || AppSettings.ProcessHistoryDepth.Value != processHistoryDepth;
            if (changed)
            {
                AppSettings.ProcessHistoryAsTab.Value = chkProcessHistoryAsTab.Checked;
                AppSettings.ProcessHistoryDepth.Value = processHistoryDepth;
                AppSettings.ProcessHistoryPanelVisible.Value = !chkProcessHistoryAsTab.Checked && processHistoryDepth > 0;
            }

            AppSettings.ConEmuTerminal.Value = ((IShellDescriptor)cboTerminal.SelectedItem).Name.ToLowerInvariant();
            base.PageToSettings();
        }

        protected override void SettingsToPage()
        {
            chkChowConsoleTab.Checked = AppSettings.ShowConEmuTab.Value;
            chkUseBrowseForFileHistory.Checked = AppSettings.UseBrowseForFileHistory.Value;
            chkUseDiffViewerForBlame.Checked = AppSettings.UseDiffViewerForBlame.Value;
            chkShowGpgInformation.Checked = AppSettings.ShowGpgInformation.Value;
            chkProcessHistoryAsTab.Checked = AppSettings.ProcessHistoryAsTab.Value;
            _NO_TRANSLATE_ProcessHistoryDepth.Value = Math.Clamp(AppSettings.ProcessHistoryDepth.Value, _NO_TRANSLATE_ProcessHistoryDepth.Minimum, _NO_TRANSLATE_ProcessHistoryDepth.Maximum);

            foreach (IShellDescriptor shell in _shellProvider.GetShells())
            {
                cboTerminal.Items.Add(shell);

                if (string.Equals(shell.Name, AppSettings.ConEmuTerminal.Value, StringComparison.InvariantCultureIgnoreCase))
                {
                    cboTerminal.SelectedItem = shell;
                }
            }

            base.SettingsToPage();
        }

        public static SettingsPageReference GetPageReference()
        {
            return new SettingsPageReferenceByType(typeof(FormBrowseRepoSettingsPage));
        }

        private void cboTerminal_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (!(cboTerminal.SelectedItem is IShellDescriptor shell))
            {
                return;
            }

            if (shell.HasExecutable)
            {
                return;
            }

            MessageBoxes.ShellNotFound(this);
            cboTerminal.SelectedIndex = _cboTerminalPreviousIndex;
        }

        private void cboTerminal_Enter(object sender, EventArgs e)
        {
            _cboTerminalPreviousIndex = cboTerminal.SelectedIndex;
        }

        private void chkProcessHistoryAsTab_InfoClicked(object sender, EventArgs e)
        {
            OsShellUtil.OpenUrlInDefaultBrowser(_processHistoryUrl);
        }
    }
}
