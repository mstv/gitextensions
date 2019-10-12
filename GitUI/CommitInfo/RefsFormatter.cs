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
        /// <summary>
        /// The number of displayed refs if the list is limited.
        ///
        /// If limited, the line "[Show all]" and an empty line are added.
        /// Hence the list needs to be limited only if it exceeds MaximumDisplayedRefsIfLimited + 2.
        /// </summary>
        private const int MaximumDisplayedRefsIfLimited = 10;

        private static readonly ILinkFactory LinkFactory = new LinkFactory();

        public static string FormatBranches(IEnumerable<string> branches, bool showAsLinks, bool limit)
        {
            var links = new List<string>();
            bool truncated = false;

            const string remotesPrefix = "remotes/";

            // Include local branches if explicitly requested or when needed to decide whether to show remotes
            bool getLocal = AppSettings.CommitInfoShowContainedInBranchesLocal ||
                            AppSettings.CommitInfoShowContainedInBranchesRemoteIfNoLocal;

            // Include remote branches if requested
            bool getRemote = AppSettings.CommitInfoShowContainedInBranchesRemote ||
                             AppSettings.CommitInfoShowContainedInBranchesRemoteIfNoLocal;
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

                    if (limit && links.Count == MaximumDisplayedRefsIfLimited + 2)
                    {
                        links.RemoveRange(MaximumDisplayedRefsIfLimited, 2);
                        truncated = true;
                        break; // from foreach
                    }

                    links.Add(branchText);
                }

                if (branchIsLocal && AppSettings.CommitInfoShowContainedInBranchesRemoteIfNoLocal)
                {
                    allowRemote = false;
                }
            }

            return ToString(links, Strings.ContainedInBranches, Strings.ContainedInNoBranch, "branches", truncated);
        }

        public static string FormatTags(IReadOnlyList<string> tags, bool showAsLinks, bool limit)
        {
            bool truncate = limit && tags.Count > MaximumDisplayedRefsIfLimited + 2;
            var links = FormatTags(truncate ? tags.Take(MaximumDisplayedRefsIfLimited) : tags);
            return ToString(links, Strings.ContainedInTags, Strings.ContainedInNoTag, "tags", truncate);

            IEnumerable<string> FormatTags(IEnumerable<string> tags_)
            {
                return tags_.Select(s => showAsLinks ? LinkFactory.CreateTagLink(s) : WebUtility.HtmlEncode(s));
            }
        }

        private static string ToString(IEnumerable<string> links, string prefix, string textIfEmpty, string refsType, bool truncated)
        {
            if (links.Any())
            {
                var sb = new StringBuilder().AppendLine(WebUtility.HtmlEncode(prefix)).Append(links.Join(Environment.NewLine));
                if (truncated)
                {
                    sb.AppendLine().AppendLine(LinkFactory.CreateShowAllLink(refsType));
                }

                return sb.ToString();
            }

            return WebUtility.HtmlEncode(textIfEmpty);
        }
    }
}