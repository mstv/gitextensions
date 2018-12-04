using System.Linq;

namespace GitUI.UserControls.RevisionGrid.Graph
{
    public class LaneInfo
    {
        public LaneInfo(RevisionGraphSegment startSegment, LaneInfo? derivedFrom)
        {
            StartRevision = derivedFrom is null ? startSegment.Child : startSegment.Parent;

            int colorSeed = StartRevision.Objectid.GetHashCode();
            if (derivedFrom is null)
            {
                colorSeed ^= startSegment.Parent.Objectid.GetHashCode();
            }

            do
            {
                Color = RevisionGraphLaneColor.GetColorForLane(colorSeed);
                ++colorSeed;
            }
            while (Color == derivedFrom?.Color);

            IsMergeLane = startSegment.Parent.Children.Count() > 1 && startSegment.Child.Parents.Count() > 1;
        }

        public int Color { get; private set; }

        public bool IsMergeLane { get; private set; }

        public RevisionGraphRevision StartRevision { get; private set; }

        public int StartScore => StartRevision.Score;
    }
}
