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
    public sealed class RefsFormatter
    {
        private static readonly ILinkFactory LinkFactory = new LinkFactory();

        public static string FormatBranches(IEnumerable<string> branches, bool showAsLinks)
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

            if (links.Any())
            {
                return WebUtility.HtmlEncode(Strings.ContainedInBranches) + " " + links.Join(", ");
            }

            return WebUtility.HtmlEncode(Strings.ContainedInNoBranch);
        }

        public static string FormatTags(IEnumerable<string> tags, bool showAsLinks)
        {
            var tagString = tags
                .Select(s => showAsLinks ? LinkFactory.CreateTagLink(s) : WebUtility.HtmlEncode(s)).Join(", ");

            if (!string.IsNullOrEmpty(tagString))
            {
                return WebUtility.HtmlEncode(Strings.ContainedInTags) + " " + tagString;
            }

            return WebUtility.HtmlEncode(Strings.ContainedInNoTag);
        }
    }
}