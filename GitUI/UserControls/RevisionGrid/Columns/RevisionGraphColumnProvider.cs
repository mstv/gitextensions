using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using GitCommands;
using GitExtUtils.GitUI;
using GitUI.UserControls.RevisionGrid.Graph;
using GitUIPluginInterfaces;
using Microsoft;

namespace GitUI.UserControls.RevisionGrid.Columns
{
    internal sealed class RevisionGraphColumnProvider : ColumnProvider
    {
        private const int MaxLanes = 40;

        private static readonly int LaneLineWidth = DpiUtil.Scale(2);
        private static readonly int LaneWidth = DpiUtil.Scale(16);
        private static readonly int NodeDimension = DpiUtil.Scale(10);

        private readonly LaneInfoProvider _laneInfoProvider;
        private readonly RevisionGridControl _grid;
        private readonly RevisionGraph _revisionGraph;
        private readonly GraphCache _graphCache = new();

        private RevisionGraphDrawStyleEnum _revisionGraphDrawStyleCache;
        private RevisionGraphDrawStyleEnum _revisionGraphDrawStyle;

        private struct SegmentLanes
        {
            internal int StartLane;
            internal int CenterLane;
            internal int EndLane;
            internal bool DrawFromStart;
            internal bool DrawToEnd;

            /// <summary>
            /// Go perpendicalur through the center in order to avoid crossing independend nodes
            /// </summary>
            internal bool DrawPerpendicular;
        }

        public RevisionGraphColumnProvider(RevisionGridControl grid, RevisionGraph revisionGraph, IGitRevisionSummaryBuilder gitRevisionSummaryBuilder)
            : base("Graph")
        {
            _grid = grid;
            _revisionGraph = revisionGraph;
            _laneInfoProvider = new LaneInfoProvider(new LaneNodeLocator(_revisionGraph), gitRevisionSummaryBuilder);

            // TODO is it worth creating a lighter-weight column type?

            Column = new DataGridViewTextBoxColumn
            {
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                Resizable = DataGridViewTriState.False,
                MinimumWidth = LaneWidth
            };
        }

        public RevisionGraphDrawStyleEnum RevisionGraphDrawStyle
        {
            get
            {
                if (_revisionGraphDrawStyle == RevisionGraphDrawStyleEnum.HighlightSelected)
                {
                    return RevisionGraphDrawStyleEnum.HighlightSelected;
                }

                if (AppSettings.RevisionGraphDrawNonRelativesGray)
                {
                    return RevisionGraphDrawStyleEnum.DrawNonRelativesGray;
                }

                return RevisionGraphDrawStyleEnum.Normal;
            }
            set { _revisionGraphDrawStyle = value; }
        }

        public override void OnCellPainting(DataGridViewCellPaintingEventArgs e, GitRevision revision, int rowHeight, in CellStyle style)
        {
            if (AppSettings.ShowRevisionGridGraphColumn &&
                e.State.HasFlag(DataGridViewElementStates.Visible) &&
                e.RowIndex >= 0 &&
                _revisionGraph.Count != 0 &&
                _revisionGraph.Count > e.RowIndex &&
                PaintGraphCell(e.RowIndex, e.CellBounds, e.Graphics))
            {
                e.Handled = true;
            }

            return;

            bool PaintGraphCell(int rowIndex, Rectangle cellBounds, Graphics graphics)
            {
                // Draws the required row into _graphBitmap, or retrieves an equivalent one from the cache.

                int height = _graphCache.Capacity * rowHeight;
                int width = Column.Width;

                if (width <= 0 || height <= 0)
                {
                    return false;
                }

                _graphCache.Allocate(width, height, LaneWidth);

                // Compute how much the head needs to move to show the requested item.
                int neededHeadAdjustment = rowIndex - _graphCache.Head;
                if (neededHeadAdjustment > 0)
                {
                    neededHeadAdjustment -= _graphCache.Capacity - 1;
                    if (neededHeadAdjustment < 0)
                    {
                        neededHeadAdjustment = 0;
                    }
                }

                int newRows = _graphCache.Count < _graphCache.Capacity
                    ? (rowIndex - _graphCache.Count) + 1
                    : 0;

                // Adjust the head of the cache
                _graphCache.Head = _graphCache.Head + neededHeadAdjustment;
                _graphCache.HeadRow = (_graphCache.HeadRow + neededHeadAdjustment) % _graphCache.Capacity;
                if (_graphCache.HeadRow < 0)
                {
                    _graphCache.HeadRow = _graphCache.Capacity + _graphCache.HeadRow;
                }

                int start;
                int end;
                if (newRows > 0)
                {
                    start = _graphCache.Head + _graphCache.Count;
                    _graphCache.Count = Math.Min(_graphCache.Count + newRows, _graphCache.Capacity);
                    end = _graphCache.Head + _graphCache.Count;
                }
                else if (neededHeadAdjustment > 0)
                {
                    end = _graphCache.Head + _graphCache.Count;
                    start = Math.Max(_graphCache.Head, end - neededHeadAdjustment);
                }
                else if (neededHeadAdjustment < 0)
                {
                    start = _graphCache.Head;
                    end = start + Math.Min(_graphCache.Capacity, -neededHeadAdjustment);
                }
                else
                {
                    // Item already in the cache
                    CreateRectangle();
                    return true;
                }

                DrawVisibleGraph();
                CreateRectangle();
                return true;

                void CreateRectangle()
                {
                    Rectangle cellRect = new(
                        0,
                        ((_graphCache.HeadRow + rowIndex - _graphCache.Head) % _graphCache.Capacity) * rowHeight,
                        width,
                        rowHeight);

                    graphics.DrawImage(
                        _graphCache.GraphBitmap,
                        cellBounds,
                        cellRect,
                        GraphicsUnit.Pixel);
                }

                void DrawVisibleGraph()
                {
                    // Getting RevisionGraphDrawStyle results in call to AppSettings. This is not very cheap, cache.
                    _revisionGraphDrawStyleCache = RevisionGraphDrawStyle;

                    for (int index = start; index < end; index++)
                    {
                        // Get the x,y value of the current item's upper left in the cache
                        int curCacheRow = (_graphCache.HeadRow + index - _graphCache.Head) % _graphCache.Capacity;
                        int x = ColumnLeftMargin;
                        int y = curCacheRow * rowHeight;

                        Validates.NotNull(_graphCache.GraphBitmapGraphics);

                        Rectangle laneRect = new(0, y, width, rowHeight);
                        Region oldClip = _graphCache.GraphBitmapGraphics.Clip;

                        if (index > 0 && (index == start || curCacheRow == 0))
                        {
                            // Draw previous row first. Clip top to row. We also need to clear the area
                            // before we draw since nothing else would clear the top 1/2 of the item to draw.
                            _graphCache.GraphBitmapGraphics.RenderingOrigin = new Point(x, y - rowHeight);
                            _graphCache.GraphBitmapGraphics.Clip = new Region(laneRect);
                            _graphCache.GraphBitmapGraphics.Clear(Color.Transparent);
                            DrawItem(_graphCache.GraphBitmapGraphics, index - 1);
                            _graphCache.GraphBitmapGraphics.Clip = oldClip;
                        }

                        if (index == end - 1)
                        {
                            // Use a custom clip for the last row
                            _graphCache.GraphBitmapGraphics.Clip = new Region(laneRect);
                        }

                        _graphCache.GraphBitmapGraphics.RenderingOrigin = new Point(x, y);

                        DrawItem(_graphCache.GraphBitmapGraphics, index);

                        _graphCache.GraphBitmapGraphics.Clip = oldClip;
                    }
                }

                void DrawItem(Graphics g, int index)
                {
                    SmoothingMode oldSmoothingMode = g.SmoothingMode;
                    Region oldClip = g.Clip;

                    int top = g.RenderingOrigin.Y;
                    Rectangle laneRect = new(0, top, width, rowHeight);
                    Region newClip = new(laneRect);
                    newClip.Intersect(oldClip);
                    g.Clip = newClip;
                    g.Clear(Color.Transparent);

                    DrawItem();

                    // Restore graphics options
                    g.Clip = oldClip;
                    g.SmoothingMode = oldSmoothingMode;

                    return;

                    void DrawItem()
                    {
                        if (index > _revisionGraph.GetCachedCount())
                        {
                            return;
                        }

                        IRevisionGraphRow? currentRow = _revisionGraph.GetSegmentsForRow(index);
                        if (currentRow is null)
                        {
                            return;
                        }

                        IRevisionGraphRow? previousRow = index < 1 ? null : _revisionGraph.GetSegmentsForRow(index - 1);
                        IRevisionGraphRow? nextRow = _revisionGraph.GetSegmentsForRow(index + 1);

                        int halfRowHeight = rowHeight / 2;
                        int halfPerpendicularHeight = rowHeight / 3;
                        int centerY = top + halfRowHeight;
                        int startY = centerY - rowHeight;
                        int endY = centerY + rowHeight;

                        LaneInfo? currentRowRevisionLaneInfo = null;

                        foreach (RevisionGraphSegment revisionGraphSegment in currentRow.Segments.Reverse().OrderBy(s => s.Child.IsRelative))
                        {
                            SegmentLanes lanes
                                = GetLanes(revisionGraphSegment, previousRow, currentRow, nextRow, () => _revisionGraph.GetSegmentsForRow(index + 2), li => currentRowRevisionLaneInfo = li);

                            if (!lanes.DrawFromStart && !lanes.DrawToEnd)
                            {
                                continue;
                            }

                            int startX = g.RenderingOrigin.X + (int)((lanes.StartLane + 0.5) * LaneWidth);
                            int centerX = g.RenderingOrigin.X + (int)((lanes.CenterLane + 0.5) * LaneWidth);
                            int endX = g.RenderingOrigin.X + (int)((lanes.EndLane + 0.5) * LaneWidth);

                            List<Point> points = new();

                            if (lanes.DrawFromStart)
                            {
                                points.Add(new(startX, startY));

                                bool drawPreviousPerpendicular = true;
                                if (index >= 2)
                                {
                                    Validates.NotNull(previousRow);
                                    SegmentLanes previousLanes
                                        = GetLanes(revisionGraphSegment, _revisionGraph.GetSegmentsForRow(index - 2), previousRow, currentRow, () => nextRow, setLaneInfo: null);
                                    drawPreviousPerpendicular = !previousLanes.DrawFromStart || previousLanes.DrawPerpendicular;
                                }

                                if (drawPreviousPerpendicular)
                                {
                                    points.Add(new(startX, startY + halfPerpendicularHeight));
                                }

                                if (lanes.DrawPerpendicular)
                                {
                                    points.Add(new(centerX, centerY - halfPerpendicularHeight));
                                }
                            }

                            points.Add(new(centerX, centerY));

                            if (lanes.DrawToEnd)
                            {
                                if (lanes.DrawPerpendicular)
                                {
                                    points.Add(new(centerX, centerY + halfPerpendicularHeight));
                                }

                                Validates.NotNull(nextRow);
                                SegmentLanes nextLanes
                                    = GetLanes(revisionGraphSegment, currentRow, nextRow, _revisionGraph.GetSegmentsForRow(index + 2), () => _revisionGraph.GetSegmentsForRow(index + 3), setLaneInfo: null);
                                if (!nextLanes.DrawToEnd || nextLanes.DrawPerpendicular)
                                {
                                    points.Add(new(endX, endY - halfPerpendicularHeight));
                                }

                                points.Add(new(endX, endY));
                            }

                            using Pen lanePen = new(GetBrushForLaneInfo(revisionGraphSegment.LaneInfo, revisionGraphSegment.Child.IsRelative), LaneLineWidth);
                            g.SmoothingMode = SmoothingMode.AntiAlias;
                            g.DrawCurve(lanePen, points.ToArray(), 0.0f);
                        }

                        if (currentRow.GetCurrentRevisionLane() < MaxLanes)
                        {
                            int centerX = g.RenderingOrigin.X + (int)((currentRow.GetCurrentRevisionLane() + 0.5) * LaneWidth);
                            Rectangle nodeRect = new(centerX - (NodeDimension / 2), centerY - (NodeDimension / 2), NodeDimension, NodeDimension);

                            bool square = currentRow.Revision.HasRef;
                            bool hasOutline = currentRow.Revision.IsCheckedOut;

                            Brush brush = GetBrushForLaneInfo(currentRowRevisionLaneInfo, currentRow.Revision.IsRelative);
                            if (square)
                            {
                                g.SmoothingMode = SmoothingMode.None;
                                g.FillRectangle(brush, nodeRect);
                            }
                            else //// Circle
                            {
                                g.SmoothingMode = SmoothingMode.AntiAlias;
                                g.FillEllipse(brush, nodeRect);
                            }

                            if (hasOutline)
                            {
                                nodeRect.Inflate(1, 1);

                                Color outlineColor = SystemColors.WindowText;

                                using Pen pen = new(outlineColor, 2);
                                if (square)
                                {
                                    g.SmoothingMode = SmoothingMode.None;
                                    g.DrawRectangle(pen, nodeRect);
                                }
                                else //// Circle
                                {
                                    g.SmoothingMode = SmoothingMode.AntiAlias;
                                    g.DrawEllipse(pen, nodeRect);
                                }
                            }
                        }
                    }

                    static SegmentLanes GetLanes(RevisionGraphSegment revisionGraphSegment,
                        IRevisionGraphRow? previousRow,
                        IRevisionGraphRow currentRow,
                        IRevisionGraphRow? nextRow,
                        Func<IRevisionGraphRow?> getNextNextRow,
                        Action<LaneInfo?>? setLaneInfo)
                    {
                        SegmentLanes lanes = new();
                        lanes.StartLane = -10;
                        lanes.CenterLane = -10;
                        lanes.EndLane = -10;

                        if (revisionGraphSegment.Parent == currentRow.Revision)
                        {
                            // This lane ends here
                            lanes.StartLane = RevisionGraphColumnProvider.GetLaneForRow(previousRow, revisionGraphSegment);
                            lanes.CenterLane = RevisionGraphColumnProvider.GetLaneForRow(currentRow, revisionGraphSegment);
                            setLaneInfo?.Invoke(revisionGraphSegment.LaneInfo);
                        }
                        else
                        {
                            if (revisionGraphSegment.Child == currentRow.Revision)
                            {
                                // This lane starts here
                                lanes.CenterLane = RevisionGraphColumnProvider.GetLaneForRow(currentRow, revisionGraphSegment);
                                lanes.EndLane = RevisionGraphColumnProvider.GetLaneForRow(nextRow, revisionGraphSegment);
                                setLaneInfo?.Invoke(revisionGraphSegment.LaneInfo);
                            }
                            else
                            {
                                // This lane crosses
                                lanes.StartLane = GetLaneForRow(previousRow, revisionGraphSegment);
                                lanes.CenterLane = GetLaneForRow(currentRow, revisionGraphSegment);
                                lanes.EndLane = GetLaneForRow(nextRow, revisionGraphSegment);
                            }

                            if (nextRow is not null)
                            {
                                ////RevisionGraphColumnProvider.StraightenSegment(revisionGraphSegment, getNextNextRow, nextRow, ref lanes.EndLane, lanes.CenterLane);
                            }
                        }

                        lanes.DrawFromStart = lanes.CenterLane >= 0 && lanes.StartLane >= 0 && (lanes.StartLane <= MaxLanes || lanes.CenterLane <= MaxLanes);
                        lanes.DrawToEnd = lanes.CenterLane >= 0 && lanes.EndLane >= 0 && (lanes.EndLane <= MaxLanes || lanes.CenterLane <= MaxLanes);

                        lanes.DrawPerpendicular = lanes.CenterLane == lanes.StartLane || lanes.StartLane < 0
                            || lanes.CenterLane == lanes.EndLane || lanes.EndLane < 0
                            || (lanes.StartLane < lanes.CenterLane && lanes.EndLane < lanes.CenterLane)
                            || (lanes.StartLane > lanes.CenterLane && lanes.EndLane > lanes.CenterLane);

                        return lanes;
                    }
                }
            }
        }

        private static void StraightenSegment(RevisionGraphSegment revisionGraphSegment, Func<IRevisionGraphRow?> getNextNextRow, IRevisionGraphRow nextRow, ref int endLane, int centerLane)
        {
            // Try to detect this:
            // | | |<-- centerLane (currentRow)
            // |/ /
            // * |<---- endLane (nextRow)
            // |\ \
            // | | |<-- nextNextLane (nextNextRow)
            //
            // And change it into this:
            // | | |
            // |/  |
            // *   |
            // |\  |
            // | | |
            //
            // also if the distance is > 1 but only if the other distance is exactly 1
            if (centerLane > endLane)
            {
                int straightenedEndLane = endLane + 1;
                int nextNextLane = GetLaneForRow(getNextNextRow(), revisionGraphSegment);
                if ((nextNextLane == straightenedEndLane) || (nextNextLane > straightenedEndLane && centerLane == straightenedEndLane))
                {
                    nextRow.MoveLanesRight(endLane);
                    endLane = straightenedEndLane;
                }
            }
        }

        private Brush GetBrushForLaneInfo(LaneInfo? laneInfo, bool isRelative)
        {
            if (!isRelative && (_revisionGraphDrawStyleCache is RevisionGraphDrawStyleEnum.DrawNonRelativesGray or RevisionGraphDrawStyleEnum.HighlightSelected))
            {
                return RevisionGraphLaneColor.NonRelativeBrush;
            }

            Validates.NotNull(laneInfo);
            return RevisionGraphLaneColor.GetBrushForLane(laneInfo.Color);
        }

        private static int GetLaneForRow(IRevisionGraphRow? row, RevisionGraphSegment revisionGraphRevision)
        {
            if (row is not null)
            {
                return row.GetLaneIndexForSegment(revisionGraphRevision);
            }

            return -1;
        }

        public override void Clear()
        {
            _revisionGraph.Clear();
            _graphCache.Clear();
            _graphCache.Reset();
        }

        public override void Refresh(int rowHeight, in VisibleRowRange range)
        {
            // Hide graph column when there it is disabled OR when a filter is active
            // allowing for special case when history of a single file is being displayed
            Column.Visible
                = AppSettings.ShowRevisionGridGraphColumn &&
                  !_grid.ShouldHideGraph(inclBranchFilter: false);
            if (!Column.Visible)
            {
                return;
            }

            _graphCache.Reset();
            Column.Width = CalculateGraphColumnWidth(range, Column.Width, Column.MinimumWidth);
        }

        public override void OnColumnWidthChanged(DataGridViewColumnEventArgs e)
        {
            _graphCache.Reset();
        }

        public void HighlightBranch(ObjectId id)
        {
            _revisionGraph.HighlightBranch(id);
        }

        public override void OnVisibleRowsChanged(in VisibleRowRange range)
        {
            if (!Column.Visible)
            {
                return;
            }

            // Keep an extra page in the cache
            _graphCache.AdjustCapacity((range.Count * 2) + 1);
            Column.Width = CalculateGraphColumnWidth(range, Column.Width, Column.MinimumWidth);
        }

        // TODO when rendering, if we notice a row has too many lanes, trigger updating the column's width

        private int CalculateGraphColumnWidth(in VisibleRowRange range, int currentWidth, int minimumWidth)
        {
            int laneCount = range.Select(index => _revisionGraph.GetSegmentsForRow(index))
                                 .WhereNotNull()
                                 .Select(laneRow => laneRow.GetLaneCount())
                                 .DefaultIfEmpty()
                                 .Max();

            laneCount = Math.Min(laneCount, MaxLanes);
            int columnWidth = (LaneWidth * laneCount) + ColumnLeftMargin;
            if (columnWidth > minimumWidth)
            {
                return columnWidth;
            }

            return minimumWidth + ColumnLeftMargin;
        }

        public bool TryGetToolTip(DataGridViewCellMouseEventArgs e, GitRevision revision)
        {
            string? toolTip;
            return TryGetToolTip(e, revision, out toolTip);
        }

        public override bool TryGetToolTip(DataGridViewCellMouseEventArgs e, GitRevision revision, [NotNullWhen(returnValue: true)] out string? toolTip)
        {
            if (e.X >= ColumnLeftMargin && LaneWidth >= 0 && e.RowIndex >= 0)
            {
                int lane = (e.X - ColumnLeftMargin) / LaneWidth;
                toolTip = _laneInfoProvider.GetLaneInfo(e.RowIndex, lane);
                return true;
            }

            toolTip = default;
            return false;
        }
    }

    public sealed class GraphCache
    {
        private Bitmap? _graphBitmap;
        private Graphics? _graphBitmapGraphics;

        public Bitmap? GraphBitmap => _graphBitmap;
        public Graphics? GraphBitmapGraphics => _graphBitmapGraphics;

        /// <summary>
        /// The 'slot' that is the head of the circular bitmap.
        /// </summary>
        public int Head { get; set; } = -1;

        /// <summary>
        /// The node row that is in the head slot.
        /// </summary>
        public int HeadRow { get; set; }

        /// <summary>
        /// Number of elements in the cache.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Number of elements allowed in the cache. Is based on control height.
        /// </summary>
        public int Capacity { get; private set; }

        public void AdjustCapacity(int capacity)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException();
            }

            Capacity = capacity;
        }

        public void Allocate(int width, int height, int laneWidth)
        {
            if (_graphBitmap is not null && _graphBitmap.Width >= width && _graphBitmap.Height == height)
            {
                return;
            }

            if (_graphBitmap is not null)
            {
                _graphBitmap.Dispose();
                _graphBitmap = null;
            }

            if (_graphBitmapGraphics is not null)
            {
                _graphBitmapGraphics.Dispose();
                _graphBitmapGraphics = null;
            }

            _graphBitmap = new Bitmap(
                Math.Max(width, laneWidth * 3),
                height,
                PixelFormat.Format32bppPArgb);
            _graphBitmapGraphics = Graphics.FromImage(_graphBitmap);
            _graphBitmapGraphics.SmoothingMode = SmoothingMode.AntiAlias;

            // With SmoothingMode != None it is better to use PixelOffsetMode.HighQuality
            // e.g. to avoid shrinking rectangles, ellipses and etc. by 1 px from right bottom
            _graphBitmapGraphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            Head = 0;
            Count = 0;
        }

        public void Clear()
        {
            Head = -1;
            HeadRow = 0;
        }

        public void Reset()
        {
            Head = 0;
            Count = 0;
        }
    }
}
