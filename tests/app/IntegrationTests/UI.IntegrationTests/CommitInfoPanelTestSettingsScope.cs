using GitCommands;
using GitCommands.Settings;

namespace GitExtensions.UITests;

/// <summary>
/// Restores <see cref="AppSettings"/> changed for CommitInfo / FormBrowse integration tests.
/// </summary>
internal sealed class CommitInfoPanelTestSettingsScope : IDisposable
{
    private readonly bool _commitInfoShowContainedInBranchesLocal;
    private readonly bool _commitInfoShowContainedInBranchesRemote;
    private readonly bool _commitInfoShowContainedInBranchesRemoteIfNoLocal;
    private readonly bool _commitInfoShowContainedInTags;
    private readonly bool _commitInfoShowTagThisCommitDerivesFrom;
    private readonly bool _showAnnotatedTagsMessages;
    private readonly CommitInfoPosition _commitInfoPosition;
    private readonly bool _revisionGraphShowArtificialCommits;

    private CommitInfoPanelTestSettingsScope(
        bool disableAsyncCommitInfoData,
        CommitInfoPosition? commitInfoPosition,
        bool? revisionGraphShowArtificialCommits)
    {
        _commitInfoShowContainedInBranchesLocal = AppSettings.CommitInfoShowContainedInBranchesLocal;
        _commitInfoShowContainedInBranchesRemote = AppSettings.CommitInfoShowContainedInBranchesRemote;
        _commitInfoShowContainedInBranchesRemoteIfNoLocal = AppSettings.CommitInfoShowContainedInBranchesRemoteIfNoLocal;
        _commitInfoShowContainedInTags = AppSettings.CommitInfoShowContainedInTags;
        _commitInfoShowTagThisCommitDerivesFrom = AppSettings.CommitInfoShowTagThisCommitDerivesFrom;
        _showAnnotatedTagsMessages = AppSettings.ShowAnnotatedTagsMessages;
        _commitInfoPosition = AppSettings.CommitInfoPosition;
        _revisionGraphShowArtificialCommits = AppSettings.RevisionGraphShowArtificialCommits;

        if (disableAsyncCommitInfoData)
        {
            AppSettings.CommitInfoShowContainedInBranchesLocal.Value = false;
            AppSettings.CommitInfoShowContainedInBranchesRemote.Value = false;
            AppSettings.CommitInfoShowContainedInBranchesRemoteIfNoLocal.Value = false;
            AppSettings.CommitInfoShowContainedInTags.Value = false;
            AppSettings.CommitInfoShowTagThisCommitDerivesFrom.Value = false;
            AppSettings.ShowAnnotatedTagsMessages.Value = false;
        }

        if (commitInfoPosition is not null)
        {
            AppSettings.CommitInfoPosition.Value = commitInfoPosition.Value;
        }

        if (revisionGraphShowArtificialCommits is not null)
        {
            AppSettings.RevisionGraphShowArtificialCommits.Value = revisionGraphShowArtificialCommits.Value;
        }
    }

    public static CommitInfoPanelTestSettingsScope ForMinimalCommitInfoDataLoad()
        => new(disableAsyncCommitInfoData: true, commitInfoPosition: null, revisionGraphShowArtificialCommits: null);

    public static CommitInfoPanelTestSettingsScope ForBrowseRepositorySwitch()
        => new(
            disableAsyncCommitInfoData: true,
            commitInfoPosition: CommitInfoPosition.BelowList,
            revisionGraphShowArtificialCommits: false);

    public void Dispose()
    {
        AppSettings.CommitInfoShowContainedInBranchesLocal.Value = _commitInfoShowContainedInBranchesLocal;
        AppSettings.CommitInfoShowContainedInBranchesRemote.Value = _commitInfoShowContainedInBranchesRemote;
        AppSettings.CommitInfoShowContainedInBranchesRemoteIfNoLocal.Value = _commitInfoShowContainedInBranchesRemoteIfNoLocal;
        AppSettings.CommitInfoShowContainedInTags.Value = _commitInfoShowContainedInTags;
        AppSettings.CommitInfoShowTagThisCommitDerivesFrom.Value = _commitInfoShowTagThisCommitDerivesFrom;
        AppSettings.ShowAnnotatedTagsMessages.Value = _showAnnotatedTagsMessages;
        AppSettings.CommitInfoPosition.Value = _commitInfoPosition;
        AppSettings.RevisionGraphShowArtificialCommits.Value = _revisionGraphShowArtificialCommits;
    }
}
