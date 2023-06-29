namespace GitUI.UserControls.RevisionGrid.Graph.Rendering
{
    internal readonly ref struct Context
    {
        public readonly Graphics G;
        public readonly Pen Pen;
        public readonly Size CellSize;

        public Context(Graphics g, Pen pen, int laneWidth, int rowHeight)
        {
            G = g;
            Pen = pen;
            CellSize = new Size(laneWidth, rowHeight);
        }
    }
}
