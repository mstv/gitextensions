namespace GitUI.Models;

public sealed record RunProcessInfo(
    string Executable,
    string Arguments,
    string Output,
    DateTime FinishTime);
