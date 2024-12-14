using GitExtensions.Extensibility.Git;

namespace GitUI.UserControls;

internal interface IFileStatusSorter
{
    TreeNode Sort(IEnumerable<GitItemStatus> statuses, Func<GitItemStatus, TreeNode> createNode);
}
