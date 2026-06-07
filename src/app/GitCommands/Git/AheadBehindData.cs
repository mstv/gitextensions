using System.Text;

namespace GitCommands.Git;

public readonly record struct AheadBehindData(string Branch,  string RemoteRef, string AheadCount, string BehindCount)
{
    // gone: "plumbing" expression, see https://git-scm.com/docs/git-for-each-ref#Documentation/git-for-each-ref.txt-upstream
    public static readonly string Gone = "gone";

    /// <summary>
    ///  Returns a string representation of the ahead/behind data, with arrows indicating the direction. If the branch is gone, it returns "✗".
    /// </summary>
    /// <param name="reverse">If true, the direction of the arrows is reversed. To be used when displaying the data for a remote branch.</param>
    public string ToDisplay(bool reverse = false)
    {
        if (AheadCount == Gone)
        {
            return "✗";
        }

        bool isBehind = BehindCount.Length > 0;

        if (AheadCount == "0" && !isBehind)
        {
            return reverse ? "0↓↑" : "0↑↓";
        }

        StringBuilder sb = new();
        if (AheadCount.Length > 0 && AheadCount != "0")
        {
            sb.Append(AheadCount).Append(reverse ? '↓' : '↑');
            if (isBehind)
            {
                sb.Append(' ');
            }
        }

        if (isBehind)
        {
            sb.Append(BehindCount).Append(reverse ? '↑' : '↓');
        }

        return sb.ToString();
    }
}
