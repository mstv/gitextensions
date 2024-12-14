using GitExtensions.Extensibility.Git;

namespace GitUI.UserControls;

internal interface IFileStatusSorter
{
    TreeNode Sort(IEnumerable<GitItemStatus> statuses, bool flat, Func<GitItemStatus, TreeNode> createNode);
}
