using GitCommands.Remotes;
using GitCommands.Settings;
using GitUIPluginInterfaces;

namespace GitCommands.ExternalLinks;

public interface IGitRevisionExternalLinksParser
{
    /// <summary>
    ///  Parses a revision for external links using the provided pre-loaded remotes.
    /// </summary>
    /// <param name="revision">The revision to parse.</param>
    /// <param name="settings">The effective distributed settings for the current repository.</param>
    /// <param name="remotes">
    ///  The remotes for the current repository, loaded before any async work to avoid
    ///  reading a stale or switched module from a background thread.
    /// </param>
    IEnumerable<ExternalLink> Parse(GitRevision revision, DistributedSettings settings, IReadOnlyList<ConfigFileRemote> remotes);
}

public sealed class GitRevisionExternalLinksParser : IGitRevisionExternalLinksParser
{
    private readonly IConfiguredLinkDefinitionsProvider _effectiveLinkDefinitionsProvider;
    private readonly IExternalLinkRevisionParser _externalLinkRevisionParser;

    public GitRevisionExternalLinksParser(IConfiguredLinkDefinitionsProvider effectiveLinkDefinitionsProvider, IExternalLinkRevisionParser externalLinkRevisionParser)
    {
        _effectiveLinkDefinitionsProvider = effectiveLinkDefinitionsProvider;
        _externalLinkRevisionParser = externalLinkRevisionParser;
    }

    public IEnumerable<ExternalLink> Parse(GitRevision revision, DistributedSettings settings, IReadOnlyList<ConfigFileRemote> remotes)
    {
        IReadOnlyList<ExternalLinkDefinition> definitions = _effectiveLinkDefinitionsProvider.Get(settings);
        return definitions.Where(definition => definition.Enabled)
                          .SelectMany(definition => _externalLinkRevisionParser.Parse(revision, definition, remotes));
    }
}
