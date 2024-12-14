namespace GitExtUtils.GitUI;

public static class TreeViewExtensions
{
    public static IEnumerable<TreeNode> Groups(this TreeView? treeView)
    {
        if (treeView is null)
        {
            yield break;
        }

        foreach (TreeNode node in treeView.Nodes)
        {
            yield return node;
        }
    }

    public static IEnumerable<TreeNode> Items(this TreeView? treeView)
    {
        if (treeView is null)
        {
            yield break;
        }

        foreach (TreeNode node in Recurse(treeView.Nodes))
        {
            yield return node;
        }
    }

    public static IEnumerable<TreeNode> Items(this TreeNode? node)
    {
        if (node is null)
        {
            yield break;
        }

        yield return node;

        foreach (TreeNode subNode in Recurse(node.Nodes))
        {
            yield return subNode;
        }
    }

    public static IEnumerable<T> ItemTags<T>(this TreeView? treeView) where T : class
        => treeView.Items().ItemTags<T>();

    public static IEnumerable<T> ItemTags<T>(this TreeNode? node) where T : class
        => node.Items().ItemTags<T>();

    /// <summary>
    /// <para>For practical purposes: The last <see cref="TreeNode"/> added to selection.</para>
    /// <para>Actually: Focused item if selected, otherwise last item in <see cref="SelectedItems"/> list.</para>
    /// </summary>
    public static TreeNode? LastSelectedItem(this TreeView treeView)
        => treeView.SelectedNode ?? treeView.SelectedItems().LastOrDefault();

    public static IReadOnlyList<int> SelectedIndices(this TreeView? treeView)
        => treeView?.SelectedNode is null ? [] : [treeView.SelectedNode.Index];

    public static IEnumerable<TreeNode> SelectedItems(this TreeView? treeView)
        => treeView.Items().Where(node => node.IsSelected);

    public static IEnumerable<T> SelectedItemTags<T>(this TreeView treeView) where T : class
        => treeView.SelectedItems().ItemTags<T>();

    private static IEnumerable<TreeNode> Recurse(TreeNodeCollection nodes)
    {
        foreach (TreeNode node in nodes)
        {
            foreach (TreeNode treeNode in node.Items())
            {
                yield return treeNode;
            }
        }
    }

    /// <summary>
    ///  Returns the Tag of the nodes which can be casted to T - without iterating subnodes.
    /// </summary>
    private static IEnumerable<T> ItemTags<T>(this IEnumerable<TreeNode> nodes) where T : class
        => nodes.Select(node => node.Tag as T).Where(value => value is not null);
}
