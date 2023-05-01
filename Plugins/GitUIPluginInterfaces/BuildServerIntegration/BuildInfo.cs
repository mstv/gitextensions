namespace GitUIPluginInterfaces.BuildServerIntegration
{
    public class BuildInfo
    {
        public enum BuildStatus
        {
            Unknown,
            InProgress,
            Success,
            Failure,
            Unstable,
            Stopped
        }

        public string? Id { get; set; }
        public DateTime StartDate { get; set; }
        public long? Duration { get; set; }
        public BuildStatus Status { get; set; }
        public string? Description { get; set; }
        public IReadOnlyList<ObjectId> CommitHashList { get; set; } = Array.Empty<ObjectId>();
        public string? Url { get; set; }
        public bool ShowInBuildReportTab { get; set; } = true;
        public string? Tooltip { get; set; }
        public string? PullRequestUrl { get; set; }

        public string StatusIcon
        {
            get
            {
                switch (Status)
                {
                    case BuildStatus.Success:
                        return "✔";
                    case BuildStatus.Failure:
                        return "❌";
                    case BuildStatus.InProgress:
                        return "▶️";
                    case BuildStatus.Stopped:
                        return "⏹️";
                    case BuildStatus.Unstable:
                        return "❗";
                    default:
                        return "❓";
                }
            }
        }
    }
}
