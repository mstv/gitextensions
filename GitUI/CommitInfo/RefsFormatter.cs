using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using GitCommands;
using ResourceManager;

namespace GitUI.CommitInfo
{
    public static class RefsFormatter
    {
        private const int MaximumDisplayedRefs = 10;

        private static readonly ILinkFactory LinkFactory = new LinkFactory();

        public static string FormatBranches(IReadOnlyList<string> branches, bool showAsLinks, bool limit)
            => ToString(branches, l => FormatBranches(l, showAsLinks), Strings.ContainedInBranches, Strings.ContainedInNoBranch, "branches", limit);

        public static string FormatTags(IReadOnlyList<string> tags, bool showAsLinks, bool limit)
            => ToString(tags, l => FormatTags(l, showAsLinks), Strings.ContainedInTags, Strings.ContainedInNoTag, "tags", limit);

        private static IEnumerable<string> FormatBranches(IEnumerable<string> branches, bool showAsLinks)
        {
            const string remotesPrefix = "remotes/";

            // Include local branches if explicitly requested or when needed to decide whether to show remotes
            bool getLocal = AppSettings.CommitInfoShowContainedInBranchesLocal ||
                            AppSettings.CommitInfoShowContainedInBranchesRemoteIfNoLocal;

            // Include remote branches if requested
            bool getRemote = AppSettings.CommitInfoShowContainedInBranchesRemote ||
                             AppSettings.CommitInfoShowContainedInBranchesRemoteIfNoLocal;
            var links = new List<string>();
            bool allowLocal = AppSettings.CommitInfoShowContainedInBranchesLocal;
            bool allowRemote = getRemote;

            foreach (var branch in branches)
            {
                string noPrefixBranch = branch;
                bool branchIsLocal;
                if (getLocal && getRemote)
                {
                    // "git branch -a" prefixes remote branches with "remotes/"
                    // It is possible to create a local branch named "remotes/origin/something"
                    // so this check is not 100% reliable.
                    // This shouldn't be a big problem if we're only displaying information.
                    // This could be solved by listing local and remote branches separately.
                    branchIsLocal = !branch.StartsWith(remotesPrefix);
                    if (!branchIsLocal)
                    {
                        noPrefixBranch = branch.Substring(remotesPrefix.Length);
                    }
                }
                else
                {
                    branchIsLocal = !getRemote;
                }

                if ((branchIsLocal && allowLocal) || (!branchIsLocal && allowRemote))
                {
                    var branchText = showAsLinks
                        ? LinkFactory.CreateBranchLink(noPrefixBranch)
                        : WebUtility.HtmlEncode(noPrefixBranch);

                    links.Add(branchText);
                }

                if (branchIsLocal && AppSettings.CommitInfoShowContainedInBranchesRemoteIfNoLocal)
                {
                    allowRemote = false;
                }
            }

            return links;
        }

        public static IEnumerable<string> FormatTags(IEnumerable<string> tags, bool showAsLinks)
        {
            return tags.Select(s => showAsLinks ? LinkFactory.CreateTagLink(s) : WebUtility.HtmlEncode(s));
        }

        private static string ToString(IReadOnlyList<string> refs, Func<IEnumerable<string>, IEnumerable<string>> formatRefs,
                                       string prefix, string textIfEmpty, string refsType, bool limit)
        {
            bool truncate = limit && refs.Count > MaximumDisplayedRefs;
            var links = formatRefs(truncate ? refs.Take(MaximumDisplayedRefs) : refs);
            if (links.Any())
            {
                var sb = new StringBuilder().AppendLine(WebUtility.HtmlEncode(prefix)).Append(links.Join(Environment.NewLine));
                if (truncate)
                {
                    sb.AppendLine().AppendLine(LinkFactory.CreateShowAllLink(refsType));
                }

                return sb.ToString();
            }

            return WebUtility.HtmlEncode(textIfEmpty);
        }
    }
}