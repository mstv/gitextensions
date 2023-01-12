namespace GitUI.UserControls.RevisionGrid.Graph
{
    public class LaneInfo
    {
        public LaneInfo(RevisionGraphSegment startSegment, RevisionGraphSegment? segmentToTheLeft)
        {
            StartRevision = startSegment.Child;
            Color = GetColor(colorSeed: StartRevision.Objectid.GetHashCode() ^ startSegment.Parent.Objectid.GetHashCode(), segmentToTheLeft);
        }

        public LaneInfo(RevisionGraphSegment startSegment, RevisionGraphSegment? segmentToTheLeft, LaneInfo derivedFrom)
        {
            StartRevision = startSegment.Parent;
            Color = GetColor(colorSeed: StartRevision.Objectid.GetHashCode(), segmentToTheLeft, derivedFrom.Color);
        }

        public int Color { get; }

        public RevisionGraphRevision StartRevision { get; }

        public int StartScore => StartRevision.Score;

        private static int GetColor(int colorSeed, RevisionGraphSegment? segmentToTheLeft, int? derivedFromColor = null)
        {
            int? leftLaneColor = segmentToTheLeft?.LaneInfo.Color;
            for (; ; ++colorSeed)
            {
                int color = RevisionGraphLaneColor.GetColorForLane(colorSeed);
                if (color != leftLaneColor && color != derivedFromColor)
                {
                    return color;
                }
            }
        }
    }
}
