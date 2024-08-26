using FluentAssertions;
using GitUI.Editor.Diff;
using ICSharpCode.TextEditor.Document;

namespace GitUITests.Editor.Diff;

[TestFixture]
public class LinesMatcherTests
{
    [Test]
    public void GetAllCombinations([Range(1, 4)] int firstEnd, [Range(1, 4)] int secondEnd)
    {
        bool[,] visited = new bool[firstEnd, secondEnd];
        foreach ((int firstIndex, int secondIndex) in LinesMatcher.GetAllCombinations(firstEnd, secondEnd))
        {
            firstIndex.Should().BeLessThan(firstEnd);
            secondIndex.Should().BeLessThan(secondEnd);
            visited[firstIndex, secondIndex].Should().BeFalse();
            visited[firstIndex, secondIndex] = true;
        }

        for (int i = 0; i < firstEnd; ++i)
        {
            for (int j = 0; j < secondEnd; ++j)
            {
                visited[i, j].Should().BeTrue($"everywhere including at {i}, {j}");
            }
        }
    }

    [Test]
    [TestCase("", new string[] { }, new int[] { })]
    [TestCase("a", new string[] { "a" }, new int[] { 0 })]
    [TestCase("a-", new string[] { "a" }, new int[] { 0 })]
    [TestCase("-a", new string[] { "a" }, new int[] { 1 })]
    [TestCase("a bc", new string[] { "a", "bc" }, new int[] { 0, 2 })]
    [TestCase("-a bc", new string[] { "a", "bc" }, new int[] { 1, 3 })]
    [TestCase("a bc-", new string[] { "a", "bc" }, new int[] { 0, 2 })]
    [TestCase("---abc---123---def_7---", new string[] { "abc", "123", "def_7" }, new int[] { 3, 9, 15 })]
    public void GetWords(string text, string[] words, int[] offsets)
    {
        (string Word, int Offset)[] result = LinesMatcher.GetWords(text, TextUtilities.IsLetterDigitOrUnderscore).ToArray();
        result.Select(LinesMatcher.SelectWord).Should().BeEquivalentTo(words);
        result.Select(LinesMatcher.SelectOffset).Should().BeEquivalentTo(offsets);
    }
}
