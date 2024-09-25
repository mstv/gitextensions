﻿using System.Diagnostics;
using System.Text;
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
    {
        if (_useGitColoring)
        {
            // Apply GE word highlighting for Patch display (may apply to Difftastic setting, if not available for a repo)
            if (AppSettings.DiffDisplayAppearance.Value != GitCommands.Settings.DiffDisplayAppearance.GitWordDiff)
            {
                MarkInlineDifferences(document);
            }

            foreach (TextMarker tm in _textMarkers)
            {
                document.MarkerStrategy.AddMarker(tm);
            }

            return;
        }

        MarkInlineDifferences(document);

        for (int line = 0; line < document.TotalNumberOfLines; line++)
        {
            LineSegment lineSegment = document.GetLineSegment(line);

            if (lineSegment.TotalLength == 0)
            {
                continue;
            }

            line = TryHighlightAddedAndDeletedLines(document, line, lineSegment);

            ProcessLineSegment(document, ref line, lineSegment, "@", AppColor.DiffSection.GetThemeColor());
            ProcessLineSegment(document, ref line, lineSegment, "\\", AppColor.DiffSection.GetThemeColor());
        }
    }

    public override bool IsSearchMatch(DiffViewerLineNumberControl lineNumbersControl, int indexInText)
        => lineNumbersControl.GetLineInfo(indexInText)?.LineType is (DiffLineType.Minus or DiffLineType.Plus or DiffLineType.MinusPlus or DiffLineType.Grep);

    public abstract string[] GetFullDiffPrefixes();

    protected readonly LinePrefixHelper LinePrefixHelper = new(new LineSegmentGetter());

    /// <summary>
    /// Parse the text in the document from line and return the added lines directly following.
    /// Overridden in the HighlightServices where GE coloring is used (AddTextHighlighting() for Patch and CombinedDiff).
    /// </summary>
    /// <param name="document">The document to analyze.</param>
    /// <param name="line">The line number to start with, updated with the last line processed.</param>
    /// <param name="found">Ref updated if any added lines were found.</param>
    /// <returns>List with the segments of added lines.</returns>
    protected virtual List<ISegment> GetAddedLines(IDocument document, ref int line, ref bool found)
        => [];

    /// <summary>
    /// Parse the text in the document from line and return the removed lines directly following.
    /// Overridden in the HighlightServices where GE coloring is used (AddTextHighlighting() for Patch and CombinedDiff).
    /// </summary>
    /// <param name="document">The document to analyze.</param>
    /// <param name="line">The line number to start with, updated with the last line processed.</param>
    /// <param name="found">Ref updated if any removed lines were found.</param>
    /// <returns>List with the segments of removed lines.</returns>
    protected virtual List<ISegment> GetRemovedLines(IDocument document, ref int line, ref bool found)
        => [];

    /// <summary>
    /// Highlight the directly following lines.
    /// Overridden in the HighlightServices where GE coloring is used (AddTextHighlighting() for Patch and CombinedDiff).
    /// </summary>
    /// <param name="document">The document to analyze.</param>
    /// <param name="line">The line number to start with.</param>
    /// <param name="lineSegment">The segment for the starting line.</param>
    /// <returns>The last line number processed.</returns>
    protected virtual int TryHighlightAddedAndDeletedLines(IDocument document, int line, LineSegment lineSegment)
        => line;

    protected void ProcessLineSegment(IDocument document, ref int line,
        LineSegment lineSegment, string prefixStr, Color color, bool invertMatch = false)
    {
        if (!DoesLineStartWith(document, lineSegment.Offset, prefixStr, invertMatch))
        {
            return;
        }

        LineSegment endLine = document.GetLineSegment(line);

        for (;
            line < document.TotalNumberOfLines
            && DoesLineStartWith(document, endLine.Offset, prefixStr, invertMatch);
            line++)
        {
            endLine = document.GetLineSegment(line);
        }

        line = Math.Max(0, line - 2);
        endLine = document.GetLineSegment(line);

        document.MarkerStrategy.AddMarker(new TextMarker(lineSegment.Offset,
            (endLine.Offset + endLine.TotalLength) -
            lineSegment.Offset, TextMarkerType.SolidBlock, color,
            ColorHelper.GetForeColorForBackColor(color)));

        return;

        bool DoesLineStartWith(IDocument document, int offset, string prefixStr, bool invertMatch)
            => invertMatch ^ LinePrefixHelper.DoesLineStartWith(document, offset, prefixStr);
    }

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
    ///  Matches related removed and added lines in a consecutive block and marks identical parts dimmed.
    /// </summary>
    private static void MarkInlineDifferences(IDocument document, IReadOnlyList<ISegment> linesRemoved, IReadOnlyList<ISegment> linesAdded, int beginOffset)
    {
        Func<ISegment, string> getText = line => document.GetText(line.Offset + beginOffset, line.Length - beginOffset);
        document.MarkerStrategy.AddMarkers(GetDifferenceMarkers(getText, linesRemoved, linesAdded, beginOffset));
    }

    private static IEnumerable<TextMarker> GetDifferenceMarkers(Func<ISegment, string> getText, IReadOnlyList<ISegment> linesRemoved, IReadOnlyList<ISegment> linesAdded, int beginOffset)
    {
        foreach ((ISegment lineRemoved, ISegment lineAdded) in LinesMatcher.FindLinePairs(getText, linesRemoved, linesAdded))
        {
            foreach (TextMarker marker in GetDifferenceMarkers(getText, lineRemoved, lineAdded, beginOffset))
            {
                yield return marker;
            }
        }
    }

    internal static IReadOnlyList<TextMarker> GetDifferenceMarkers(Func<ISegment, string> getText, ISegment lineRemoved, ISegment lineAdded, int beginOffset)
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

        Debug.WriteLine($"AddDifferenceMarkers({textRemoved}, {textAdded}, {offsetRemoved}, {offsetAdded})");

        int lengthIdenticalAtStart = 0;
        int lengthIdenticalAtEnd = 0;

        int endRemoved = textRemoved.Length;
        int endAdded = textAdded.Length;
        if (endRemoved == endAdded && textRemoved == textAdded)
        {
            lengthIdenticalAtStart = endRemoved;
            Debug.WriteLine($"AddDifferenceMarkers({textRemoved}, {textAdded}, {offsetRemoved}, {offsetAdded}) -> {lengthIdenticalAtStart}, {lengthIdenticalAtEnd}");
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

        Debug.WriteLine($"AddDifferenceMarkers({textRemoved}, {textAdded}, {offsetRemoved}, {offsetAdded}) -> {lengthIdenticalAtStart}, {lengthIdenticalAtEnd}");
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

    /// <summary>
    ///  Matches related removed and added lines in a consecutive block of a patch document and marks identical parts dimmed.
    /// </summary>
    private void MarkInlineDifferences(IDocument document)
    {
        int line = 0;
        bool found = false;

        // Skip the first pair of removed / added lines, which uses to contain the filenames - but without highlighting
        _ = GetRemovedLines(document, ref line, ref found);
        if (found)
        {
            _ = GetAddedLines(document, ref line, ref found);
        }

        // Process the next blocks of removed / added lines and mark in-line differences
        const int diffContentOffset = 1; // in order to skip the prefixes '-' / '+'
        while (line < document.TotalNumberOfLines)
        {
            found = false;

            List<ISegment> linesRemoved = GetRemovedLines(document, ref line, ref found);
            if (!found)
            {
                continue;
            }

            List<ISegment> linesAdded = GetAddedLines(document, ref line, ref found);
            if (!found)
            {
                continue;
            }

            MarkInlineDifferences(document, linesRemoved, linesAdded, diffContentOffset);
        }
    }
}
