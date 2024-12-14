using GitExtensions.Extensibility.Git;
using GitExtUtils.GitUI;

namespace GitUI.UserControls;

internal sealed class FileStatusSorter : IFileStatusSorter
{
    public TreeNode Sort(IEnumerable<GitItemStatus> statuses, Func<GitItemStatus, TreeNode> createNode)
    {
        TreeNode root = new() { Tag = "" };

        TreeNode? previousLeaf = null;
        foreach (GitItemStatus status in statuses.OrderBy(s => s, new PathFirstComparer()))
        {
            TreeNode parent = previousLeaf is null || status.IsRangeDiff
                ? root
                : GetOrCreateParent(previousLeaf, status.Path) ?? root;
            TreeNode leaf = createNode(status);
            parent.Nodes.Add(leaf);
            previousLeaf = leaf;
        }

        root.Items().ForEach(RemoveParentPath);

        return root;

        static void RemoveParentPath(TreeNode node)
        {
            if (node.Parent?.Tag is string parentPath)
            {
                if (parentPath.Length > 0 && node.Text.StartsWith(parentPath))
                {
                    node.Text = node.Text[(parentPath.Length + 1)..];
                }

                if (node.Tag is FileStatusItem item && item.Item.Path != parentPath)
                {
                    node.StateImageIndex = 0;
                }
            }
        }
    }

    private static string GetCommonPath(string a, string b)
    {
        a += '/';
        b += '/';
        for (int commonEnd = 0; ; ++commonEnd)
        {
            if (commonEnd >= a.Length || commonEnd >= b.Length || a[commonEnd] != b[commonEnd])
            {
                // Revert possible partial match
                while (commonEnd > 0 && a[--commonEnd] != '/')
                {
                }

                return a[..commonEnd];
            }
        }
    }

    private static TreeNode? GetOrCreateParent(TreeNode previousLeaf, string currentPath)
    {
        string previousPath = ((FileStatusItem)previousLeaf.Tag).Item.Path;
        if (previousPath == currentPath && (string)previousLeaf.Parent.Tag == currentPath)
        {
            return previousLeaf.Parent;
        }

        string commonPath = GetCommonPath(previousPath, currentPath);
        if (commonPath.Length == 0)
        {
            return null;
        }

        TreeNode splitCandidate = previousLeaf;
        string splitCandidatePath = previousPath;
        while (splitCandidate.Parent?.Tag is string path && path.StartsWith(commonPath))
        {
            splitCandidate = splitCandidate.Parent;
            splitCandidatePath = path;
        }

        if (splitCandidate != previousLeaf && (splitCandidatePath == currentPath || splitCandidatePath == commonPath))
        {
            return splitCandidate;
        }

        return Split(splitCandidate, splitCandidatePath, commonPath);
    }

    private static TreeNode Split(TreeNode subNode, string subNodePath, string commonPath)
    {
        TreeNode parentNode = subNode.Parent ?? throw new ArgumentNullException($"{nameof(subNode)}.{nameof(subNode.Parent)}");
        int index = parentNode.Nodes.IndexOf(subNode);
        parentNode.Nodes.Remove(subNode);
        TreeNode commonFolderNode = new(commonPath) { Tag = commonPath };
        commonFolderNode.Nodes.Add(subNode);
        parentNode.Nodes.Insert(index, commonFolderNode);
        return commonFolderNode;
    }

    internal static class TestAccessor
    {
        public static string GetCommonPath(string a, string b) => FileStatusSorter.GetCommonPath(a, b);
    }

    private sealed class PathFirstComparer : IComparer<GitItemStatus>
    {
        public int Compare(GitItemStatus l, GitItemStatus r)
            => (l, r) switch
            {
                (null, null) => 0,
                (_, null) => -1,
                (null, _) => 1,
                (_, _) when l.IsRangeDiff && r.IsRangeDiff => 0,
                (_, _) when r.IsRangeDiff => -1,
                (_, _) when l.IsRangeDiff => 1,
                _ => CompareNonNull(l, r)
            };

        private static int CompareNonNull(GitItemStatus l, GitItemStatus r)
        {
            int pathComparison = (l.Path, r.Path) switch
            {
                ("", "") => 0,
                (_, "") => -1,
                ("", _) => 1,
                _ => StringComparer.InvariantCultureIgnoreCase.Compare(l.Path, r.Path)
            };

            return pathComparison switch
            {
                -1 => r.Path.StartsWith(l.Path, StringComparison.InvariantCultureIgnoreCase) ? 1 : -1,
                1 => l.Path.StartsWith(r.Path, StringComparison.InvariantCultureIgnoreCase) ? -1 : 1,
                _ => StringComparer.InvariantCultureIgnoreCase.Compare(l.Name, r.Name)
            };
        }
    }
}
