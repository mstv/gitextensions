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
    [TestCase("", new string[] { })]
    [TestCase("a", new string[] { "a" })]
    [TestCase("a-", new string[] { "a" })]
    [TestCase("-a", new string[] { "a" })]
    [TestCase("a bc", new string[] { "a", "bc" })]
    [TestCase("-a bc", new string[] { "a", "bc" })]
    [TestCase("a bc-", new string[] { "a", "bc" })]
    [TestCase("---abc---123---def_7---", new string[] { "abc", "123", "def_7" })]
    public void GetWords(string text, string[] words)
    {
        LinesMatcher.GetWords(text, TextUtilities.IsLetterDigitOrUnderscore)
            .Should().BeEquivalentTo(words);
    }
}
