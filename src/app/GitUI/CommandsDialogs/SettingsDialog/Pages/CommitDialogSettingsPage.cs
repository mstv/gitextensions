using GitCommands;

namespace GitUI.CommandsDialogs.SettingsDialog.Pages;

public partial class CommitDialogSettingsPage : SettingsPageWithHeader
{
    public CommitDialogSettingsPage(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        InitializeComponent();
        InitializeComplete();
    }

    protected override void SettingsToPage()
    {
        chkShowErrorsWhenStagingFiles.Checked = AppSettings.ShowErrorsWhenStagingFiles;
        chkEnsureCommitMessageSecondLineEmpty.Checked = AppSettings.EnsureCommitMessageSecondLineEmpty;
        chkWriteCommitMessageInCommitWindow.Checked = AppSettings.UseFormCommitMessage;
        _NO_TRANSLATE_CommitDialogNumberOfPreviousMessages.Value = AppSettings.CommitDialogNumberOfPreviousMessages;
        chkShowCommitAndPush.Checked = AppSettings.ShowCommitAndPush;
        chkShowResetWorkTreeChanges.Checked = AppSettings.ShowResetWorkTreeChanges;
        chkShowResetAllChanges.Checked = AppSettings.ShowResetAllChanges;
        chkAutocomplete.Checked = AppSettings.ProvideAutocompletion;
        cbRememberAmendCommitState.Checked = AppSettings.RememberAmendCommitState;

        base.SettingsToPage();
    }

    protected override void PageToSettings()
    {
        AppSettings.ShowErrorsWhenStagingFiles.Value = chkShowErrorsWhenStagingFiles.Checked;
        AppSettings.EnsureCommitMessageSecondLineEmpty.Value = chkEnsureCommitMessageSecondLineEmpty.Checked;
        AppSettings.UseFormCommitMessage.Value = chkWriteCommitMessageInCommitWindow.Checked;
        AppSettings.CommitDialogNumberOfPreviousMessages.Value = (int)_NO_TRANSLATE_CommitDialogNumberOfPreviousMessages.Value;
        AppSettings.ShowCommitAndPush.Value = chkShowCommitAndPush.Checked;
        AppSettings.ShowResetWorkTreeChanges.Value = chkShowResetWorkTreeChanges.Checked;
        AppSettings.ShowResetAllChanges.Value = chkShowResetAllChanges.Checked;
        AppSettings.ProvideAutocompletion.Value = chkAutocomplete.Checked;
        AppSettings.RememberAmendCommitState.Value = cbRememberAmendCommitState.Checked;

        base.PageToSettings();
    }
}
