namespace GitUI.UserControls.RevisionGrid.Graph.Rendering
{
    internal struct SegmentLaneFlags
    {
        public bool DrawFromStart;
        public bool DrawToEnd;
        public bool DrawCenterToStartPerpendicularly;
        public bool DrawCenter;
        public bool DrawCenterPerpendicularly;
        public bool DrawCenterToEndPerpendicularly;
        public bool IsTheRevisionLane;
        public int HorizontalOffset;
    }
}
