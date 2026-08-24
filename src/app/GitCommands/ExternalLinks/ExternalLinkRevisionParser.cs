using System.Text.RegularExpressions;
using GitCommands.Remotes;
using GitUIPluginInterfaces;

namespace GitCommands.ExternalLinks;

public interface IExternalLinkRevisionParser
{
    /// <summary>
    ///  Parses a revision against a link definition using the provided pre-loaded remotes.
    /// </summary>
    /// <param name="revision">The revision to parse.</param>
    /// <param name="definition">The link definition to apply.</param>
    /// <param name="remotes">
    ///  The remotes for the current repository, loaded before any async work to avoid
    ///  reading a stale or switched module from a background thread.
    /// </param>
    IEnumerable<ExternalLink> Parse(GitRevision revision, ExternalLinkDefinition definition, IReadOnlyList<ConfigFileRemote> remotes);
}

public sealed class ExternalLinkRevisionParser : IExternalLinkRevisionParser
{
    public IEnumerable<ExternalLink> Parse(GitRevision revision, ExternalLinkDefinition definition, IReadOnlyList<ConfigFileRemote> remotes)
    {
        IEnumerable<Match?> remoteMatches = ParseRemotes(definition, remotes);
        return remoteMatches.SelectMany(remoteMatch => ParseRevision(revision, definition, remoteMatch));
    }

    private static IEnumerable<ConfigFileRemote> GetMatchingRemotes(ExternalLinkDefinition definition, IReadOnlyList<ConfigFileRemote> remotes)
    {
        if (string.IsNullOrWhiteSpace(definition.UseRemotesPattern) || definition.UseRemotesRegex?.Value is null)
        {
            return remotes;
        }

        IEnumerable<ConfigFileRemote> matchingRemotes = remotes.Where(r => definition.UseRemotesRegex.Value.IsMatch(r.Name!))
                                                        .OrderBy(r => definition.UseRemotesPattern.IndexOf(r.Name!, StringComparison.OrdinalIgnoreCase));
        if (definition.UseOnlyFirstRemote)
        {
            matchingRemotes = matchingRemotes.Take(1);
        }

        return matchingRemotes;
    }

    private static IEnumerable<Match?> ParseRemotes(ExternalLinkDefinition definition, IReadOnlyList<ConfigFileRemote> remotes)
    {
        List<Match?> allMatches = [];

        if (string.IsNullOrWhiteSpace(definition.RemoteSearchPattern) || definition.RemoteSearchPatternRegex?.Value is null)
        {
            allMatches.Add(null);
            return allMatches;
        }

        List<string> remoteUrls = [];

        IEnumerable<ConfigFileRemote> matchingRemotes = GetMatchingRemotes(definition, remotes);

        foreach (ConfigFileRemote remote in matchingRemotes)
        {
            if (definition.RemoteSearchInParts.Contains(ExternalLinkDefinition.RemotePart.URL))
            {
                if (!string.IsNullOrWhiteSpace(remote.Url))
                {
                    remoteUrls.Add(remote.Url);
                }
            }

            if (definition.RemoteSearchInParts.Contains(ExternalLinkDefinition.RemotePart.PushURL))
            {
                if (!string.IsNullOrWhiteSpace(remote.PushUrl))
                {
                    remoteUrls.Add(remote.PushUrl);
                }
            }
        }

        foreach (string url in remoteUrls.Distinct())
        {
            MatchCollection matches = definition.RemoteSearchPatternRegex.Value.Matches(url);
            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];
                if (match.Success)
                {
                    allMatches.Add(match);
                }
            }
        }

        return allMatches;
    }

    private static IEnumerable<ExternalLink> ParseRevision(GitRevision revision, ExternalLinkDefinition definition, Match? remoteMatch)
    {
        List<IEnumerable<ExternalLink>> links = [];

        if (definition.SearchInParts.Contains(ExternalLinkDefinition.RevisionPart.LocalBranches))
        {
            links.AddRange(
                revision.Refs
                    .Where(b => !b.IsRemote)
                    .Select(head => ParseRevisionPart(revision, definition, remoteMatch, head.LocalName)));
        }

        if (definition.SearchInParts.Contains(ExternalLinkDefinition.RevisionPart.RemoteBranches))
        {
            links.AddRange(
                revision.Refs
                    .Where(b => b.IsRemote)
                    .Select(head => ParseRevisionPart(revision, definition, remoteMatch, head.LocalName)));
        }

        if (definition.SearchInParts.Contains(ExternalLinkDefinition.RevisionPart.Message))
        {
            links.Add(ParseRevisionPart(revision, definition, remoteMatch, revision.Body));
        }

        return links.SelectMany(list => list);
    }

    private static IEnumerable<ExternalLink> ParseRevisionPart(GitRevision revision, ExternalLinkDefinition definition, Match? remoteMatch, string? part)
    {
        if (string.IsNullOrEmpty(definition.SearchPattern) || definition.SearchPatternRegex?.Value is null || part is null)
        {
            yield break;
        }

        List<Match> allMatches = [];

        MatchCollection matches = definition.SearchPatternRegex.Value.Matches(part);
        for (int i = 0; i < matches.Count; i++)
        {
            Match match = matches[i];
            if (match.Success)
            {
                if (string.IsNullOrEmpty(definition.NestedSearchPattern))
                {
                    allMatches.Add(match);
                }
                else if (definition.NestedSearchPatternRegex?.Value is not null)
                {
                    MatchCollection nestedMatches = definition.NestedSearchPatternRegex.Value.Matches(match.Value);

                    for (int n = 0; n < nestedMatches.Count; n++)
                    {
                        allMatches.Add(nestedMatches[n]);
                    }
                }
            }
        }

        foreach (Match match in allMatches.Where(m => m.Success))
        {
            foreach (ExternalLinkFormat format in definition.LinkFormats)
            {
                yield return format.Apply(remoteMatch, match, revision);
            }
        }
    }
}
