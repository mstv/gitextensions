using Castle.Core;
using FluentAssertions;
using GitUI.Editor.Diff;
using ICSharpCode.TextEditor.Document;

namespace GitUITests.Editor.Diff;

[TestFixture]
public class DiffHighlightServiceTests
{
    [Test]
    public void GetDifferenceMarkers()
    {
        // LineSegment is hard to create. Use TextMarker as implementation type of ISegment for this test.
        const TextMarkerType dontCare = TextMarkerType.SolidBlock;

        const string identicalPartBefore = "identical_part_before_";
        const string identicalPartAfter = "_identical_part_after";
        const string differentRemoved = "RemovedX";
        const string differentAdded = "AddedY";
        const string removedLineText = $"-{identicalPartBefore}{differentRemoved}{identicalPartAfter}";
        const string addedLineText = $"+{identicalPartBefore}{differentAdded}{identicalPartAfter}";
        const string text = $"{removedLineText}\n{addedLineText}";
        TextMarker removedLine = new(offset: text.IndexOf(removedLineText), removedLineText.Length, textMarkerType: dontCare);
        TextMarker addedLine = new(offset: text.IndexOf(addedLineText), addedLineText.Length, textMarkerType: dontCare);
        const int lineStartOffset = 1;

        IEnumerable<TextMarker> markers = DiffHighlightService.GetDifferenceMarkers(GetCharAt, removedLine, addedLine, lineStartOffset);

        TextMarker[] expectedMarkers =
        [
            new TextMarker(offset: addedLine.Offset + lineStartOffset, length: identicalPartBefore.Length, TextMarkerType.SolidBlock),
            new TextMarker(offset: addedLine.Offset + lineStartOffset + identicalPartBefore.Length + differentAdded.Length, length: identicalPartAfter.Length, TextMarkerType.SolidBlock),
            new TextMarker(offset: removedLine.Offset + lineStartOffset, length: identicalPartBefore.Length, TextMarkerType.SolidBlock),
            new TextMarker(offset: removedLine.Offset + lineStartOffset + identicalPartBefore.Length + differentRemoved.Length, length: identicalPartAfter.Length, TextMarkerType.SolidBlock),
        ];
        int index = 0;
        foreach (TextMarker marker in markers)
        {
            TextMarker expected = expectedMarkers[index++];
            marker.Offset.Should().Be(expected.Offset);
            marker.Length.Should().Be(expected.Length);
            marker.TextMarkerType.Should().Be(expected.TextMarkerType);
        }

        return;

        char GetCharAt(int offset) => text[offset];
    }
}
