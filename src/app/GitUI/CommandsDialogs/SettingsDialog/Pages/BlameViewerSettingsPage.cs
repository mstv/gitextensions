using GitCommands;
using GitExtensions.Extensibility.Settings;
using ResourceManager;

namespace GitUI.CommandsDialogs.SettingsDialog.Pages;

public partial class BlameViewerSettingsPage : SettingsPageWithHeader
{
    private readonly TranslationString _blameWarningTooltip = new("Could prevent blame to calculate the accurate line number when blaming previous revisions.");

    public BlameViewerSettingsPage(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        InitializeComponent();
        InitializeComplete();
        cbDetectMoveAndCopyInThisFile.ToolTipText = _blameWarningTooltip.Text;
        cbDetectMoveAndCopyInAllFiles.ToolTipText = _blameWarningTooltip.Text;
    }

    protected override void SettingsToPage()
    {
        cbIgnoreWhitespace.Checked = AppSettings.IgnoreWhitespaceOnBlame;
        cbDetectMoveAndCopyInThisFile.Checked = AppSettings.DetectCopyInFileOnBlame;
        cbDetectMoveAndCopyInAllFiles.Checked = AppSettings.DetectCopyInAllOnBlame;

        cbDisplayAuthorFirst.Checked = AppSettings.BlameDisplayAuthorFirst;
        cbShowAuthor.Checked = AppSettings.BlameShowAuthor;
        cbShowAuthorDate.Checked = AppSettings.BlameShowAuthorDate;
        cbShowAuthorTime.Checked = AppSettings.BlameShowAuthorTime;
        cbShowLineNumbers.Checked = AppSettings.BlameShowLineNumbers;
        cbShowOriginalFilePath.Checked = AppSettings.BlameShowOriginalFilePath;
        cbShowAuthorAvatar.Checked = AppSettings.BlameShowAuthorAvatar;

        base.SettingsToPage();
    }

    protected override void PageToSettings()
    {
        AppSettings.IgnoreWhitespaceOnBlame.Value = cbIgnoreWhitespace.Checked;
        AppSettings.DetectCopyInAllOnBlame.Value = cbDetectMoveAndCopyInAllFiles.Checked;
        AppSettings.DetectCopyInFileOnBlame.Value = cbDetectMoveAndCopyInThisFile.Checked;

        AppSettings.BlameDisplayAuthorFirst.Value = cbDisplayAuthorFirst.Checked;
        AppSettings.BlameShowAuthor.Value = cbShowAuthor.Checked;
        AppSettings.BlameShowAuthorDate.Value = cbShowAuthorDate.Checked;
        AppSettings.BlameShowAuthorTime.Value = cbShowAuthorTime.Checked;
        AppSettings.BlameShowLineNumbers.Value = cbShowLineNumbers.Checked;
        AppSettings.BlameShowOriginalFilePath.Value = cbShowOriginalFilePath.Checked;
        AppSettings.BlameShowAuthorAvatar.Value = cbShowAuthorAvatar.Checked;

        base.PageToSettings();
    }

    public static SettingsPageReference GetPageReference()
    {
        return new SettingsPageReferenceByType(typeof(BlameViewerSettingsPage));
    }
}
