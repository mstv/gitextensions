﻿#nullable enable

using GitExtensions.Extensibility.Git;
using GitUI.UserControls;
using Microsoft;

namespace GitUI;

partial class FileStatusList
{
    internal sealed class StatusSorter : IStatusSorter
    {
        public TreeNode CreateTreeSortedByPath(IEnumerable<GitItemStatus> statuses, bool flat, Func<GitItemStatus, TreeNode> createNode)
        {
            TreeNode root = CreateParent(RelativePath.From(""));

            TreeNode parent = root;
            foreach (GitItemStatus status in statuses.OrderBy(s => s, new PathFirstComparer()))
            {
                parent = flat ? root : GetOrCreateParent(parent, status.Path, root);
                TreeNode leaf = createNode(status);
                parent.Nodes.Add(leaf);
            }

            root.Items().ForEach(RemoveParentPath);

            return root;

            static void RemoveParentPath(TreeNode node)
            {
                if (node.Parent?.Tag is RelativePath parentPath)
                {
                    if (parentPath.Length > 0 && node.Text.StartsWith(parentPath.Value))
                    {
                        node.Text = node.Text[(parentPath.Length + 1)..];
                    }
                }
            }
        }

        private static TreeNode CreateParent(RelativePath relativePath) => new(text: relativePath.Value) { Tag = relativePath };

        private static RelativePath GetCommonPath(RelativePath relativePathA, RelativePath relativePathB)
        {
            string a = $"{relativePathA}/";
            string b = $"{relativePathB}/";
            for (int commonEnd = 0; ; ++commonEnd)
            {
                if (commonEnd >= a.Length || commonEnd >= b.Length || a[commonEnd] != b[commonEnd])
                {
                    // Revert possible partial match
                    while (commonEnd > 0 && a[--commonEnd] != '/')
                    {
                    }

                    return RelativePath.From(a[..commonEnd]);
                }
            }
        }

        private static TreeNode GetOrCreateParent(TreeNode previousParent, RelativePath currentPath, TreeNode root)
        {
            Validates.NotNull(previousParent.Tag);
            RelativePath previousPath = (RelativePath)previousParent.Tag;
            if (previousPath == currentPath)
            {
                return previousParent;
            }

            RelativePath commonPath = GetCommonPath(previousPath, currentPath);
            TreeNode commonParent = GetOrCreateCommonParent();
            if (currentPath == commonPath)
            {
                return commonParent;
            }

            TreeNode parent = CreateParent(currentPath);
            commonParent.Nodes.Add(parent);
            return parent;

            TreeNode GetOrCreateCommonParent()
            {
                if (commonPath.Length == 0)
                {
                    return root;
                }

                TreeNode splitCandidate = previousParent;
                RelativePath splitCandidatePath = previousPath;
                while (splitCandidate.Parent?.Tag is RelativePath path && path.Value.StartsWith(commonPath.Value))
                {
                    splitCandidate = splitCandidate.Parent;
                    splitCandidatePath = path;
                }

                return splitCandidatePath == commonPath
                    ? splitCandidate
                    : Split(splitCandidate, commonPath);
            }
        }

        private static TreeNode Split(TreeNode subNode, RelativePath commonPath)
        {
            TreeNode parentNode = subNode.Parent ?? throw new ArgumentNullException($"{nameof(subNode)}.{nameof(subNode.Parent)}");
            int index = parentNode.Nodes.IndexOf(subNode);
            parentNode.Nodes.Remove(subNode);
            TreeNode commonFolderNode = CreateParent(commonPath);
            commonFolderNode.Nodes.Add(subNode);
            parentNode.Nodes.Insert(index, commonFolderNode);
            return commonFolderNode;
        }

        private sealed class PathFirstComparer : IComparer<GitItemStatus>
        {
            public int Compare(GitItemStatus? l, GitItemStatus? r)
                => (l, r) switch
                {
                    (null, null) => 0,
                    (_, null) => -1,
                    (null, _) => 1,
                    _ => CompareNonNull(l, r)
                };

            private static int CompareNonNull(GitItemStatus l, GitItemStatus r)
            {
                int pathComparison = (l.Path.Value, r.Path.Value) switch
                {
                    ("", "") => 0,
                    (_, "") => -1,
                    ("", _) => 1,
                    _ => StringComparer.InvariantCultureIgnoreCase.Compare(l.Path.Value, r.Path.Value)
                };

                return pathComparison switch
                {
                    -1 => r.Path.Value.StartsWith(l.Path.Value, StringComparison.InvariantCultureIgnoreCase) ? 1 : -1,
                    1 => l.Path.Value.StartsWith(r.Path.Value, StringComparison.InvariantCultureIgnoreCase) ? -1 : 1,
                    _ => StringComparer.InvariantCultureIgnoreCase.Compare(l.Name, r.Name)
                };
            }
        }

        internal static class TestAccessor
        {
            public static string GetCommonPath(string a, string b) => StatusSorter.GetCommonPath(RelativePath.From(a), RelativePath.From(b)).Value;
        }
    }
}
