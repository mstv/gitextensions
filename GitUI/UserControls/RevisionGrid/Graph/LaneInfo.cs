namespace GitUI.UserControls.RevisionGrid.Graph
{
    public class LaneInfo
    {
        public LaneInfo(RevisionGraphSegment startSegment, LaneInfo? derivedFrom, RevisionGraphSegment? segmentToTheLeft)
        {
            StartRevision = derivedFrom is null ? startSegment.Child : startSegment.Parent;

            int colorSeed = StartRevision.Objectid.GetHashCode();
            if (derivedFrom is null)
            {
                colorSeed ^= startSegment.Parent.Objectid.GetHashCode();
            }

            int? leftLaneColor = segmentToTheLeft?.LaneInfo.Color;
            do
            {
                Color = RevisionGraphLaneColor.GetColorForLane(colorSeed);
                ++colorSeed;
            }
            while (Color == derivedFrom?.Color || Color == leftLaneColor);
        }

        public int Color { get; init; }

        public RevisionGraphRevision StartRevision { get; init; }

        public int StartScore => StartRevision.Score;
    }
}
