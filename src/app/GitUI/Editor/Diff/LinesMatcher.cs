using System.Diagnostics;
using GitExtensions.Extensibility;
using ICSharpCode.TextEditor.Document;

namespace GitUI.Editor.Diff;

internal static class LinesMatcher
{
    internal static IEnumerable<(ISegment? RemovedLine, ISegment? AddedLine)> FindLinePairs(
        Func<ISegment, string> getText, IReadOnlyList<ISegment> removedLines, IReadOnlyList<ISegment> addedLines)
    {
        int numberOfCombinations = removedLines.Count * addedLines.Count;
        if (numberOfCombinations < 1)
        {
            yield break;
        }

        // Do not try to match more lines than usually visible at the same time, because it costs O(n^2) operations
        const int maxCombinations = 100 * 100;
        if (numberOfCombinations == 1 || numberOfCombinations > maxCombinations)
        {
            int minCount = Math.Min(removedLines.Count, addedLines.Count);
            for (int i = 0; i < minCount; ++i)
            {
                yield return (removedLines[i], addedLines[i]);
            }

            yield break;
        }

        LineData[] removed = removedLines.Select(line => new LineData(line, getText(line))).ToArray();
        LineData[] added = addedLines.Select(line => new LineData(line, getText(line))).ToArray();

        foreach ((ISegment?, ISegment?) linePair in FindLinePairs(removed, added))
        {
            yield return linePair;
        }
    }

    private static IEnumerable<(ISegment? RemovedLine, ISegment? AddedLine)> FindLinePairs(LineData[] removed, LineData[] added)
    {
        (int removedIndex, int addedIndex) = FindBestMatch(removed, added);

        if (removedIndex > 0 && addedIndex > 0)
        {
            foreach ((ISegment?, ISegment?) linePair in FindLinePairs(removed[0..removedIndex], added[0..addedIndex]))
            {
                yield return linePair;
            }
        }
        else
        {
            foreach (LineData unmatchedRemoved in removed[0..removedIndex])
            {
                yield return (unmatchedRemoved.Line, AddedLine: null);
            }

            foreach (LineData unmatchedAdded in added[0..addedIndex])
            {
                yield return (RemovedLine: null, unmatchedAdded.Line);
            }
        }

        yield return (removed[removedIndex].Line, added[addedIndex].Line);

        ++removedIndex;
        ++addedIndex;
        if (removedIndex < removed.Length && addedIndex < added.Length)
        {
            foreach ((ISegment?, ISegment?) linePair in FindLinePairs(removed[removedIndex..], added[addedIndex..]))
            {
                yield return linePair;
            }
        }
        else
        {
            foreach (LineData unmatchedRemoved in removed[removedIndex..])
            {
                yield return (unmatchedRemoved.Line, AddedLine: null);
            }

            foreach (LineData unmatchedAdded in added[addedIndex..])
            {
                yield return (RemovedLine: null, unmatchedAdded.Line);
            }
        }
    }

    private static (int RemovedIndex, int AddedIndex) FindBestMatch(LineData[] removed, LineData[] added)
    {
        // search longest match of Trimmed
        (LineData longestMatchingRemoved, int matchingAddedIndex)
            = removed.Select(r => (r, addedIndex: added.IndexOf(a => a.Trimmed == r.Trimmed)))
                     .MaxBy(pair => pair.addedIndex < 0 ? -1 : pair.r.Trimmed.Length);
        if (matchingAddedIndex >= 0)
        {
            return (Array.IndexOf(removed, longestMatchingRemoved), matchingAddedIndex);
        }

        int removedMaxScoreIndex = 0;
        int addedMaxScoreIndex = 0;
        float maxScore = -1;
        foreach ((int removedIndex, int addedIndex) in GetAllCombinations(removed.Length, added.Length))
        {
            float score = GetWordMatchScore(removed[removedIndex], added[addedIndex]);
            if (maxScore < score)
            {
                maxScore = score;
                removedMaxScoreIndex = removedIndex;
                addedMaxScoreIndex = addedIndex;
                if (maxScore == 1)
                {
                    return (removedMaxScoreIndex, addedMaxScoreIndex);
                }
            }
        }

        return (removedMaxScoreIndex, addedMaxScoreIndex);

        static float GetWordMatchScore(LineData r, LineData a)
        {
            if (r.Words.Count == 0 || a.Words.Count == 0)
            {
                return -1;
            }

            return (float)r.Words.Intersect(a.Words).Count() / Math.Max(r.Words.Count, a.Words.Count);
        }
    }

    internal static IEnumerable<(int FirstIndex, int SecondIndex)> GetAllCombinations(int firstEnd, int secondEnd)
    {
        // upper left half including prinicipal diagonal
        for (int diagonalIndex = 0; diagonalIndex < firstEnd; ++diagonalIndex)
        {
            int diagonalEnd = Math.Min(diagonalIndex + 1, secondEnd);
            for (int secondIndex = 0; secondIndex < diagonalEnd; ++secondIndex)
            {
                yield return (FirstIndex: diagonalIndex - secondIndex, secondIndex);
            }
        }

        // lower right half
        for (int diagonalIndex = 1; diagonalIndex < secondEnd; ++diagonalIndex)
        {
            int diagonalEnd = Math.Min(firstEnd + diagonalIndex, secondEnd);
            for (int secondIndex = diagonalIndex; secondIndex < diagonalEnd; ++secondIndex)
            {
                yield return (FirstIndex: firstEnd - 1 + diagonalIndex - secondIndex, secondIndex);
            }
        }
    }

    internal static IEnumerable<string> GetWords(string text, Func<char, bool> isWordChar)
    {
        int length = text.Length;
        int start = 0;
        while (true)
        {
            for (; ; ++start)
            {
                if (start >= length)
                {
                    // no (more) word found, exit function
                    yield break;
                }

                if (isWordChar(text[start]))
                {
                    break;
                }
            }

            // start of word found

            for (int end = start + 1; ; ++end)
            {
                if (end >= length || !isWordChar(text[end]))
                {
                    // word end found, yield and find next word
                    yield return text[start..end];
                    start = end + 1;
                    break;
                }
            }
        }
    }

    [DebuggerDisplay("{Line.Offset}: {Trimmed}")]
    private readonly struct LineData
    {
        internal ISegment Line { get; }
        internal string Full { get; }
        internal string Trimmed { get; }
        internal IReadOnlySet<string> Words { get; }

        internal LineData(ISegment line, string text)
        {
            Line = line;
            Full = text;
            Trimmed = text.Trim();
            Words = GetWords(Trimmed, TextUtilities.IsLetterDigitOrUnderscore).ToHashSet();
        }
    }
}
