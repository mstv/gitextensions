using GitCommands;
using GitExtensions.Extensibility.Settings;
using GitUI.UserControls.RevisionGrid;
using ResourceManager;

namespace GitUI.CommandsDialogs.SettingsDialog.Pages;

public partial class DiffViewerSettingsPage : SettingsPageWithHeader
{
    private readonly TranslationString _saveCurrentViewSettingsAsDefaultTooltip = new("""
        Saves all current view settings as the default for future sessions.
        Note: The checkboxes 'Remember the "xyz" preference' only affect the running instance
        as long as the default has not been saved. These preference values are held in memory
        and must be explicitly saved to become persistent defaults.
        """);

    public DiffViewerSettingsPage(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        InitializeComponent();
        InitializeComplete();

        chkShowDiffForAllParents.Text = TranslatedStrings.ShowDiffForAllParentsText;
        chkShowDiffForAllParents.ToolTipText = TranslatedStrings.ShowDiffForAllParentsTooltip;
        chkContScrollToNextFileOnlyWithAlt.Text = TranslatedStrings.ContScrollToNextFileOnlyWithAlt;
    }

    protected override void SettingsToPage()
    {
        chkRememberIgnoreWhiteSpacePreference.Checked = AppSettings.RememberIgnoreWhiteSpacePreference;
        chkOmitUninterestingDiff.Checked = AppSettings.OmitUninterestingDiff;
        chkRememberShowEntireFilePreference.Checked = AppSettings.RememberShowEntireFilePreference;
        chkRememberDiffAppearancePreference.Checked = AppSettings.RememberDiffDisplayAppearance;
        chkRememberShowNonPrintingCharsPreference.Checked = AppSettings.RememberShowNonPrintingCharsPreference;
        chkRememberNumberOfContextLines.Checked = AppSettings.RememberNumberOfContextLines;
        chkRememberShowSyntaxHighlightingInDiff.Checked = AppSettings.RememberShowSyntaxHighlightingInDiff;
        chkOpenSubmoduleDiffInSeparateWindow.Checked = AppSettings.OpenSubmoduleDiffInSeparateWindow;
        chkContScrollToNextFileOnlyWithAlt.Checked = AppSettings.AutomaticContinuousScroll;
        chkShowDiffForAllParents.Checked = AppSettings.ShowDiffForAllParents;
        chkShowAllCustomDiffTools.Checked = AppSettings.ShowAvailableDiffTools;
        VerticalRulerPosition.Value = AppSettings.DiffVerticalRulerPosition.Value;
        MaxDiffFileSize.Value = Math.Clamp(AppSettings.MaxDiffFileSizeMb, MaxDiffFileSize.Minimum, MaxDiffFileSize.Maximum);
        chkUseGitColoring.Checked = AppSettings.UseGitColoring;
        chkUseGEThemeGitColoring.Checked = AppSettings.ReverseGitColoring;
        chkUseGEThemeGitColoring.Enabled = chkUseGitColoring.Checked;

        base.SettingsToPage();
    }

    protected override void PageToSettings()
    {
        AppSettings.RememberIgnoreWhiteSpacePreference.Value = chkRememberIgnoreWhiteSpacePreference.Checked;
        AppSettings.OmitUninterestingDiff.Value = chkOmitUninterestingDiff.Checked;
        AppSettings.RememberShowEntireFilePreference.Value = chkRememberShowEntireFilePreference.Checked;
        AppSettings.RememberDiffDisplayAppearance.Value = chkRememberDiffAppearancePreference.Checked;
        AppSettings.RememberShowNonPrintingCharsPreference.Value = chkRememberShowNonPrintingCharsPreference.Checked;
        AppSettings.RememberNumberOfContextLines.Value = chkRememberNumberOfContextLines.Checked;
        AppSettings.RememberShowSyntaxHighlightingInDiff.Value = chkRememberShowSyntaxHighlightingInDiff.Checked;
        AppSettings.OpenSubmoduleDiffInSeparateWindow.Value = chkOpenSubmoduleDiffInSeparateWindow.Checked;
        AppSettings.AutomaticContinuousScroll.Value = chkContScrollToNextFileOnlyWithAlt.Checked;
        AppSettings.ShowDiffForAllParents.Value = chkShowDiffForAllParents.Checked;
        AppSettings.ShowAvailableDiffTools.Value = chkShowAllCustomDiffTools.Checked;
        AppSettings.DiffVerticalRulerPosition.Value = (int)VerticalRulerPosition.Value;
        AppSettings.MaxDiffFileSizeMb = (int)MaxDiffFileSize.Value;
        AppSettings.UseGitColoring.Value = chkUseGitColoring.Checked;
        AppSettings.ReverseGitColoring.Value = chkUseGEThemeGitColoring.Checked;

        base.PageToSettings();
    }

    private void chkUseGitColoring_CheckedChanged(object sender, EventArgs e)
        => chkUseGEThemeGitColoring.Enabled = chkUseGitColoring.Checked;

    private void btnSaveCurrentViewSettingsAsDefault_Click(object sender, EventArgs e)
    {
        RevisionGridMenuCommands.SaveCurrentViewSettingsAsDefault();
    }

    public static SettingsPageReference GetPageReference()
    {
        return new SettingsPageReferenceByType(typeof(DiffViewerSettingsPage));
    }
}
