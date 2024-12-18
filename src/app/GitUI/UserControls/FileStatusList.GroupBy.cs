using GitExtensions.Extensibility.Git;

namespace GitUI;

partial class FileStatusList
{
    private record GroupBy(
        Func<GitItemStatus, string> GetGroupKey,
        Func<string, string> GetImageKey,
        Func<string, string> GetLabel,
        bool Reverse = false);
}
