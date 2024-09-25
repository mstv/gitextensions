﻿using System.Text;
using GitCommands;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitExtUtils.GitUI.Theming;
using GitUI.Theming;
using ICSharpCode.TextEditor.Document;

namespace GitUI.Editor.Diff;

/// <summary>
/// Common class for highlighting of diff style files.
/// </summary>
public abstract class DiffHighlightService : TextHighlightService
{
    protected readonly bool _useGitColoring;
    protected readonly List<TextMarker> _textMarkers = [];
    protected DiffLinesInfo _diffLinesInfo;

    public DiffHighlightService(ref string text, bool useGitColoring)
    {
        _useGitColoring = useGitColoring;
        SetText(ref text);
    }

    public static IGitCommandConfiguration GetGitCommandConfiguration(IGitModule module, bool useGitColoring, string command)
    {
        if (!useGitColoring)
        {
            // Use default
            return null;
        }

        GitCommandConfiguration commandConfiguration = new();
        IReadOnlyList<GitConfigItem> items = GitCommandConfiguration.Default.Get(command);
        foreach (GitConfigItem cfg in items)
        {
            commandConfiguration.Add(cfg, command);
        }

        // https://git-scm.com/docs/git-diff#Documentation/git-diff.txt---color-moved-wsltmodesgt
        // Disable by default, document that this can be enabled.
        SetIfUnsetInGit(key: "diff.colorMovedWS", value: "no");

        // https://git-scm.com/docs/git-diff#Documentation/git-diff.txt-diffwordRegex
        // Set to "minimal" diff unless configured.
        SetIfUnsetInGit(key: "diff.wordRegex", value: "\"[a-z0-9_]+|.\"");

        // dimmed-zebra highlights borders better than the default "zebra"
        SetIfUnsetInGit(key: "diff.colorMoved", value: "dimmed-zebra");

        // Use reverse color to follow GE theme
        string reverse = AppSettings.ReverseGitColoring.Value ? "reverse" : "";

        SetIfUnsetInGit(key: "color.diff.old", value: $"red {reverse}");
        SetIfUnsetInGit(key: "color.diff.new", value: $"green {reverse}");

        if (AppSettings.ReverseGitColoring.Value)
        {
            // Fix: Force black foreground to avoid that foreground is calculated to white
            GitVersion supportsBrightColors = new("2.26.0.0");
            if (module.GitVersion >= supportsBrightColors)
            {
                SetIfUnsetInGit(key: "color.diff.oldMoved", value: "black brightmagenta");
                SetIfUnsetInGit(key: "color.diff.newMoved", value: "black brightblue");
                SetIfUnsetInGit(key: "color.diff.oldMovedAlternative", value: "black brightcyan");
                SetIfUnsetInGit(key: "color.diff.newMovedAlternative", value: "black brightyellow");
            }
            else
            {
                SetIfUnsetInGit(key: "color.diff.oldMoved", value: "reverse bold magenta");
                SetIfUnsetInGit(key: "color.diff.newMoved", value: "reverse bold blue");
                SetIfUnsetInGit(key: "color.diff.oldMovedAlternative", value: "reverse bold cyan");
                SetIfUnsetInGit(key: "color.diff.newMovedAlternative", value: "reverse bold yellow");
            }
        }

        // Set dimmed colors, default is gray dimmed/italic
        SetIfUnsetInGit(key: "color.diff.oldMovedDimmed", value: $"magenta dim {reverse}");
        SetIfUnsetInGit(key: "color.diff.newMovedDimmed", value: $"blue dim {reverse}");
        SetIfUnsetInGit(key: "color.diff.oldMovedAlternativeDimmed", value: $"cyan dim {reverse}");
        SetIfUnsetInGit(key: "color.diff.newMovedAlternativeDimmed", value: $"yellow dim {reverse}");

        // range-diff
        if (command == "range-diff")
        {
            SetIfUnsetInGit(key: "color.diff.contextBold", value: $"normal bold {reverse}");
            SetIfUnsetInGit(key: "color.diff.oldBold", value: $"brightred {reverse}");
            SetIfUnsetInGit(key: "color.diff.newBold", value: $"brightgreen  {reverse}");
        }

        return commandConfiguration;

        void SetIfUnsetInGit(string key, string value)
        {
            // Note: Only check Windows, not WSL settings
            if (string.IsNullOrEmpty(module.GetEffectiveSetting(key)))
            {
                commandConfiguration.Add(new GitConfigItem(key, value), command);
            }
        }
    }

    public override void AddTextHighlighting(IDocument document)
        => document.MarkerStrategy.AddMarkers(_textMarkers);

    public override bool IsSearchMatch(DiffViewerLineNumberControl lineNumbersControl, int indexInText)
        => lineNumbersControl.GetLineInfo(indexInText)?.LineType is (DiffLineType.Minus or DiffLineType.Plus or DiffLineType.MinusPlus or DiffLineType.Grep);

    public abstract string[] GetFullDiffPrefixes();

    private void SetText(ref string text)
    {
        if (!_useGitColoring)
        {
            return;
        }

        StringBuilder sb = new(text.Length);
        AnsiEscapeUtilities.ParseEscape(text, sb, _textMarkers);

        text = sb.ToString();
    }

    /// <summary>
    /// Set highlighting for <paramref name="text"/>.
    /// The parsed added/removed lines in <see cref="_diffLinesInfo"/> is used as well as
    /// the highlighting in <see cref="_textMarkers"/> (if Git highlighting <see cref="_useGitColoring"/>),
    /// is used to mark inline differences (dim unchanged part of lines).
    /// </summary>
    /// <param name="text">The text to process.</param>
    internal void SetHighlighting(string text)
    {
        if (!_useGitColoring)
        {
            HighlightAddedAndDeletedLines(_textMarkers);
        }

        // Apply GE word highlighting for Patch display (may apply to Difftastic setting, if not available for a repo)
        if (!_useGitColoring || AppSettings.DiffDisplayAppearance.Value != GitCommands.Settings.DiffDisplayAppearance.GitWordDiff)
        {
            MarkInlineDifferences(text, _textMarkers);
        }
    }

    /// <summary>
    /// Highlight lines that are added, removed and header lines.
    /// This is an alternative configuration to use the Git diff coloring (that has more features).
    /// </summary>
    /// <param name="textMarkers">The markers to append to.</param>
    private void HighlightAddedAndDeletedLines(List<TextMarker> textMarkers)
    {
        foreach (ISegment segment in GetAllLines(DiffLineType.Minus))
        {
            textMarkers.Add(CreateTextMarker(segment, AppColor.AnsiTerminalRedBackNormal.GetThemeColor()));
        }

        foreach (ISegment segment in GetAllLines(DiffLineType.Plus))
        {
            textMarkers.Add(CreateTextMarker(segment, AppColor.AnsiTerminalGreenBackNormal.GetThemeColor()));
        }

        foreach (ISegment segment in GetAllLines(DiffLineType.Header))
        {
            textMarkers.Add(CreateTextMarker(segment, AppColor.DiffSection.GetThemeColor()));
        }

        return;

        static TextMarker CreateTextMarker(ISegment segment, Color color)
            => new(segment.Offset, segment.Length, TextMarkerType.SolidBlock, color, ColorHelper.GetForeColorForBackColor(color));
    }

    /// <summary>
    ///  Matches related removed and added lines in a consecutive block of a patch document and marks identical parts dimmed.
    /// </summary>
    private void MarkInlineDifferences(string text, List<TextMarker> textMarkers)
    {
        int index = 0;
        List<DiffLineInfo> diffLines = [.. _diffLinesInfo.DiffLines.Values.OrderBy(i => i.LineNumInDiff)];
        bool found = false;
        int insertLine = 0;
        const int diffContentOffset = 1; // in order to skip the prefixes '-' / '+'

        // Process the next blocks of removed / added diffLines and mark in-line differences
        while (index < diffLines.Count)
        {
            found = false;

            // git-diff presents the removed lines directly followed by the added in a "block"
            IReadOnlyList<ISegment> linesRemoved = GetBlockOfLines(diffLines, DiffLineType.Minus, ref index, ref found);
            IReadOnlyList<ISegment> linesAdded = GetBlockOfLines(diffLines, DiffLineType.Plus, ref index, ref found);

            IEnumerable<TextMarker> addMarkers = GetDifferenceMarkers(text, linesRemoved, linesAdded, diffContentOffset);
            textMarkers.InsertRange(insertLine, addMarkers);
            insertLine += addMarkers.Count();
        }
    }

    private List<ISegment> GetAllLines(DiffLineType diffLineType)
    => _diffLinesInfo?.DiffLines.Where(i => i.Value.LineType == diffLineType && i.Value.Segment is not null)
            .Select(i => i.Value.Segment)
            .ToList()
            ?? [];

    /// <summary>
    /// Get next block of diffLines following beginline
    /// </summary>
    /// <param name="diffLines">The parsed diffLines for the document.</param>
    /// <param name="diffLineType">The type of diffLines to find (e.g. added/removed).</param>
    /// <param name="index">The index in diffLines to start with.</param>
    /// <param name="found">If a lineInDiff was found. This is also used to get the added diffLines just after the removed.</param>
    /// <returns>The block of segments.</returns>
    private static List<ISegment> GetBlockOfLines(List<DiffLineInfo> diffLines, DiffLineType diffLineType, ref int index, ref bool found)
    {
        List<ISegment> result = [];
        int gapLines = 0;

        for (; index < diffLines.Count; ++index)
        {
            DiffLineInfo diffLine = diffLines[index];
            if (diffLine.LineType != diffLineType)
            {
                if (!found)
                {
                    // Start of block is not found yet.
                    continue;
                }

                const int maxGapLines = 5;
                if (diffLine?.LineType != DiffLineType.Context)
                {
                    // Block cannot start or continue
                    break;
                }
                else if (gapLines < maxGapLines)
                {
                    // A gap context diffLines, the block can be extended
                    ++gapLines;
                    continue;
                }

                // Block ended, no more to add (next start search here)
                break;
            }

            ArgumentNullException.ThrowIfNull(diffLine.Segment);
            gapLines = 0;
            if (diffLine.IsMovedLine)
            {
                // Ignore this line, seem to be moved
                continue;
            }

            // In block, continue to add
            found = true;
            result.Add(diffLine.Segment);
        }

        return result;
    }

    private static IEnumerable<TextMarker> GetDifferenceMarkers(string text, IReadOnlyList<ISegment> linesRemoved, IReadOnlyList<ISegment> linesAdded, int beginOffset)
    {
        foreach ((ISegment lineRemoved, ISegment lineAdded) in LinesMatcher.FindLinePairs(getText, linesRemoved, linesAdded))
        {
            foreach (TextMarker marker in GetDifferenceMarkers(getText, lineRemoved, lineAdded, beginOffset))
            {
                yield return marker;
            }
        }

        string getText(ISegment line)
        {
            int len = line.Length - beginOffset;
            if (line.Offset + len == text.Length)
            {
                // This is likely a test, where last line do not have a newline
                --len;
            }

            return text.Substring(line.Offset + beginOffset, len);
        }
    }

    internal static IEnumerable<TextMarker> GetDifferenceMarkers(Func<ISegment, string> getText, ISegment lineRemoved, ISegment lineAdded, int beginOffset)
    {
        string textRemoved = getText(lineRemoved);
        string textAdded = getText(lineAdded);
        int offsetRemoved = lineRemoved.Offset + beginOffset;
        int offsetAdded = lineAdded.Offset + beginOffset;
        List<TextMarker> markers = [];
        (int lengthIdenticalAtStart, int lengthIdenticalAtEnd) = AddDifferenceMarkers(markers, textRemoved, textAdded, offsetRemoved, offsetAdded);

        if (lengthIdenticalAtStart > 0)
        {
            markers.Add(CreateDimmedMarker(offsetRemoved, lengthIdenticalAtStart, GetRemovedBackColor()));
            markers.Add(CreateDimmedMarker(offsetAdded, lengthIdenticalAtStart, GetAddedBackColor()));
        }

        if (lengthIdenticalAtEnd > 0)
        {
            markers.Add(CreateDimmedMarker(offsetRemoved + textRemoved.Length - lengthIdenticalAtEnd, lengthIdenticalAtEnd, GetRemovedBackColor()));
            markers.Add(CreateDimmedMarker(offsetAdded + textAdded.Length - lengthIdenticalAtEnd, lengthIdenticalAtEnd, GetAddedBackColor()));
        }

        return markers;
    }

    private static (int LengthIdenticalAtStart, int LengthIdenticalAtEnd) AddDifferenceMarkers(List<TextMarker> markers, string textRemoved, string textAdded, int offsetRemoved, int offsetAdded)
    {
        // removed:             added:              "d" stands for "deleted" / "i" for "inserted" -> anchor marker in added / removed
        // "d b R a "           " b A a i"          split at "b" (stands for "before")
        // 1.                   1.
        // "d ""b"" R a "       " ""b"" A a i"      split at "a" (stands for "after")
        // 5.     2.            5.    2.
        // "d ""b"" R ""a"" "   " ""b"" A ""a"" i"  join identical
        //        4.      3.          4.      3.
        // "d"" b ""R"" a """   """ b ""A"" a ""i"

        int lengthIdenticalAtStart = 0;
        int lengthIdenticalAtEnd = 0;

        int endRemoved = textRemoved.Length;
        int endAdded = textAdded.Length;
        if (endRemoved == endAdded && textRemoved == textAdded)
        {
            lengthIdenticalAtStart = endRemoved;
            return (lengthIdenticalAtStart, lengthIdenticalAtEnd);
        }

        (string? commonWord, int startIndexIdenticalRemoved, int startIndexIdenticalAdded) = LinesMatcher.FindBestMatch(textRemoved, textAdded);
        if (commonWord is not null)
        {
            int lengthIdentical = commonWord.Length;

            // "LeftPart|CommonWord|RightPart"
            // "LeftPart|CommonWord|identical|Different|identical"
            // "LeftPart|CommonWord+identical" ignored  ^^^^^^^^^ -> lengthIdenticalAtEnd (final value)
            int startIndexRightPartRemoved = startIndexIdenticalRemoved + lengthIdentical;
            int startIndexRightPartAdded = startIndexIdenticalAdded + lengthIdentical;
            (int lengthIdenticalAtStartRightPart, lengthIdenticalAtEnd) = AddDifferenceMarkers(markers,
                textRemoved[startIndexRightPartRemoved..], textAdded[startIndexRightPartAdded..],
                offsetRemoved + startIndexRightPartRemoved, offsetAdded + startIndexRightPartAdded);
            lengthIdentical += lengthIdenticalAtStartRightPart;

            ////                                                             "LeftPart|CommonWord+identical"
            ////                                        "identical|Different|identical|CommonWord+identical"
            //// lengthIdenticalAtStart (final value) <- ^^^^^^^^^  ignored "identical+CommonWord+identical"
            (lengthIdenticalAtStart, int lengthIdenticalAtLeftPartEnd) = AddDifferenceMarkers(markers,
                textRemoved[..startIndexIdenticalRemoved], textAdded[..startIndexIdenticalAdded],
                offsetRemoved, offsetAdded);
            lengthIdentical += lengthIdenticalAtLeftPartEnd;
            startIndexIdenticalRemoved -= lengthIdenticalAtLeftPartEnd;
            startIndexIdenticalAdded -= lengthIdenticalAtLeftPartEnd;

            markers.Add(CreateDimmedMarker(offsetRemoved + startIndexIdenticalRemoved, lengthIdentical, GetRemovedBackColor()));
            markers.Add(CreateDimmedMarker(offsetAdded + startIndexIdenticalAdded, lengthIdentical, GetAddedBackColor()));
        }
        else
        {
            // find end of identical part at start
            int minEnd = Math.Min(endRemoved, endAdded);
            while (lengthIdenticalAtStart < minEnd
                && textRemoved[lengthIdenticalAtStart] == textAdded[lengthIdenticalAtStart])
            {
                ++lengthIdenticalAtStart;
            }

            // find start of identical part at end
            int startIndexIdenticalAtEndRemoved = endRemoved;
            int startIndexIdenticalAtEndAdded = endAdded;
            while (startIndexIdenticalAtEndRemoved > lengthIdenticalAtStart && startIndexIdenticalAtEndAdded > lengthIdenticalAtStart
                && textRemoved[startIndexIdenticalAtEndRemoved - 1] == textAdded[startIndexIdenticalAtEndAdded - 1])
            {
                --startIndexIdenticalAtEndRemoved;
                --startIndexIdenticalAtEndAdded;
                ++lengthIdenticalAtEnd;
            }

            int lengthDifferentRemoved = startIndexIdenticalAtEndRemoved - lengthIdenticalAtStart;
            int lengthDifferentAdded = startIndexIdenticalAtEndAdded - lengthIdenticalAtStart;
            if (lengthDifferentRemoved == 0 && lengthDifferentAdded > 0)
            {
                markers.Add(CreateAnchorMarker(offsetRemoved + lengthIdenticalAtStart, GetAddedForeColor()));
            }
            else if (lengthDifferentRemoved > 0 && lengthDifferentAdded == 0)
            {
                markers.Add(CreateAnchorMarker(offsetAdded + lengthIdenticalAtStart, GetRemovedForeColor()));
            }
        }

        return (lengthIdenticalAtStart, lengthIdenticalAtEnd);
    }

    private static TextMarker CreateAnchorMarker(int offset, Color color)
        => new(offset, length: 0, TextMarkerType.InterChar, color);

    private static TextMarker CreateDimmedMarker(int offset, int length, Color color)
        => CreateTextMarker(offset, length, ColorHelper.DimColor(ColorHelper.DimColor(color)));

    private static TextMarker CreateTextMarker(int offset, int length, Color color)
        => new(offset, length, TextMarkerType.SolidBlock, color, ColorHelper.GetForeColorForBackColor(color));

    private static Color GetAddedBackColor() => AppColor.AnsiTerminalGreenBackNormal.GetThemeColor();
    private static Color GetAddedForeColor() => AppColor.AnsiTerminalGreenForeBold.GetThemeColor();
    private static Color GetRemovedBackColor() => AppColor.AnsiTerminalRedBackNormal.GetThemeColor();
    private static Color GetRemovedForeColor() => AppColor.AnsiTerminalRedForeBold.GetThemeColor();
}
