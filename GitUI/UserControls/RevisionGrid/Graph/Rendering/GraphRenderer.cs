using System.Diagnostics;
using System.Drawing.Drawing2D;
using GitCommands;
using GitExtUtils.GitUI;
using GitUIPluginInterfaces;
using Microsoft;

namespace GitUI.UserControls.RevisionGrid.Graph.Rendering
{
    internal static class GraphRenderer
    {
        internal const int MaxLanes = RevisionGraph.MaxLanes;

        public static readonly int LaneLineWidth = DpiUtil.Scale(2);
        public static readonly int LaneWidth = DpiUtil.Scale(16);
        public static readonly int NodeDimension = DpiUtil.Scale(10);

        private const int _noLane = -10;

        public static void DrawItem(Graphics g, int index, int width, int rowHeight,
            Func<int, IRevisionGraphRow?> getSegmentsForRow,
            RevisionGraphDrawStyleEnum revisionGraphDrawStyle,
            ObjectId headId)
        {
            SmoothingMode oldSmoothingMode = g.SmoothingMode;
            Region oldClip = g.Clip;

            int top = g.RenderingOrigin.Y;
            Rectangle laneRect = new(0, top, width, rowHeight);
            using Region newClip = new(laneRect);
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
                IRevisionGraphRow? currentRow = getSegmentsForRow(index);
                if (currentRow is null)
                {
                    return;
                }

                IRevisionGraphRow? previousRow = getSegmentsForRow(index - 1);
                IRevisionGraphRow? nextRow = getSegmentsForRow(index + 1);

                SegmentPointsInfo p = new();
                p.Center.Y = top + (rowHeight / 2);
                p.Start.Y = p.Center.Y - rowHeight;
                p.End.Y = p.Center.Y + rowHeight;

                LaneInfo? currentRowRevisionLaneInfo = null;

                foreach (RevisionGraphSegment revisionGraphSegment in currentRow.Segments.Reverse().OrderBy(s => s.Child.IsRelative))
                {
                    SegmentLanesInfo lanes = GetLanesInfo(revisionGraphSegment, previousRow, currentRow, nextRow, li => currentRowRevisionLaneInfo = li);
                    if (!lanes.DrawFromStart && !lanes.DrawToEnd)
                    {
                        continue;
                    }

                    int originX = g.RenderingOrigin.X;
                    p.Start.X = originX + (int)((lanes.StartLane + 0.5) * LaneWidth);
                    p.Center.X = originX + (int)((lanes.CenterLane + 0.5) * LaneWidth);
                    p.End.X = originX + (int)((lanes.EndLane + 0.5) * LaneWidth);

                    Brush laneBrush = GetBrushForLaneInfo(revisionGraphSegment.LaneInfo, revisionGraphSegment.Child.IsRelative, revisionGraphDrawStyle);
                    using Pen lanePen = new(laneBrush, LaneLineWidth);
                    SegmentRenderer segmentRenderer = new(new Context(g, lanePen, LaneWidth, rowHeight));

                    if (AppSettings.DrawGraphWithDiagonals.Value)
                    {
                        Lazy<SegmentLanesInfo> previousLanes = new(() =>
                        {
                            Validates.NotNull(previousRow);
                            return GetLanesInfo(revisionGraphSegment, getSegmentsForRow(index - 2), previousRow, currentRow);
                        });
                        Lazy<SegmentLanesInfo> nextLanes = new(() =>
                        {
                            Validates.NotNull(nextRow);
                            return GetLanesInfo(revisionGraphSegment, currentRow, nextRow, getSegmentsForRow(index + 2));
                        });
                        Lazy<SegmentLanesInfo> farLanesDontMatter = null;

                        Lazy<SegmentLaneFlags> previousLaneFlags = new(() => GetDiagonalLaneFlags(previousLanes: farLanesDontMatter, currentLanes: previousLanes.Value, nextLanes: new(() => lanes)));
                        Lazy<SegmentLaneFlags> nextLaneFlags = new(() => GetDiagonalLaneFlags(previousLanes: new(() => lanes), currentLanes: nextLanes.Value, nextLanes: farLanesDontMatter));
                        SegmentLaneFlags currentLaneFlags = GetDiagonalLaneFlags(previousLanes, lanes, nextLanes);

                        DrawSegmentWithDiagonals(segmentRenderer, p, previousLaneFlags, currentLaneFlags, nextLaneFlags);
                    }
                    else
                    {
                        DrawSegmentCurvy(segmentRenderer, p, lanes);
                    }
                }

                if (currentRow.GetCurrentRevisionLane() < MaxLanes)
                {
                    int centerX = g.RenderingOrigin.X + (int)((currentRow.GetCurrentRevisionLane() + 0.5) * LaneWidth);
                    Rectangle nodeRect = new(centerX - (NodeDimension / 2), p.Center.Y - (NodeDimension / 2), NodeDimension, NodeDimension);

                    bool square = currentRow.Revision.GitRevision.Refs.Count > 0;
                    bool hasOutline = currentRow.Revision.GitRevision.ObjectId == headId;

                    Brush brush = GetBrushForLaneInfo(currentRowRevisionLaneInfo, currentRow.Revision.IsRelative, revisionGraphDrawStyle);
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
        }

        public static SegmentLanesInfo GetLanesInfo(RevisionGraphSegment revisionGraphSegment,
            IRevisionGraphRow? previousRow,
            IRevisionGraphRow currentRow,
            IRevisionGraphRow? nextRow,
            Action<LaneInfo?>? setLaneInfo = null)
        {
            Lane currentLane = currentRow.GetLaneForSegment(revisionGraphSegment);

            int startLane = _noLane;
            int centerLane = _noLane;
            int endLane = _noLane;
            bool isTheRevisionLane = true;

            // Avoid drawing the same curve twice (caused aliasing artifacts, particularly when in different colors)
            if (currentLane.Sharing == LaneSharing.Entire)
            {
                return new SegmentLanesInfo(startLane, centerLane, endLane, endLane, isTheRevisionLane, drawFromStart: false, drawToEnd: false);
            }

            centerLane = currentLane.Index;
            if (revisionGraphSegment.Parent == currentRow.Revision)
            {
                // This lane ends here
                startLane = GetLaneForRow(previousRow, revisionGraphSegment);
                setLaneInfo?.Invoke(revisionGraphSegment.LaneInfo);
            }
            else
            {
                if (revisionGraphSegment.Child == currentRow.Revision)
                {
                    // This lane starts here
                    endLane = GetLaneForRow(nextRow, revisionGraphSegment);
                    setLaneInfo?.Invoke(revisionGraphSegment.LaneInfo);
                }
                else
                {
                    // This lane crosses
                    startLane = GetLaneForRow(previousRow, revisionGraphSegment);
                    endLane = GetLaneForRow(nextRow, revisionGraphSegment);
                    isTheRevisionLane = false;
                }
            }

            int primaryEndLane = endLane;
            switch (currentLane.Sharing)
            {
                case LaneSharing.DifferentStart:
                    if (AppSettings.MergeGraphLanesHavingCommonParent.Value)
                    {
                        endLane = _noLane;
                    }
                    else if (endLane != _noLane)
                    {
                        throw new Exception($"{currentRow.Revision.Objectid.ToShortString()}: lane {centerLane} has DifferentStart but has EndLane {endLane} (StartLane {startLane})");
                    }

                    break;

                case LaneSharing.DifferentEnd:
                    if (startLane != _noLane)
                    {
                        throw new Exception($"{currentRow.Revision.Objectid.ToShortString()}: lane {centerLane} has DifferentEnd but has StartLane {startLane} (EndLane {endLane})");
                    }

                    break;
            }

            return new SegmentLanesInfo(startLane, centerLane, endLane, primaryEndLane, isTheRevisionLane,
                drawFromStart: startLane >= 0 && centerLane >= 0 && (startLane <= MaxLanes || centerLane <= MaxLanes),
                drawToEnd: endLane >= 0 && centerLane >= 0 && (endLane <= MaxLanes || centerLane <= MaxLanes));
        }

        private static SegmentLaneFlags GetDiagonalLaneFlags(Lazy<SegmentLanesInfo>? previousLanes,
            SegmentLanesInfo currentLanes,
            Lazy<SegmentLanesInfo>? nextLanes)
        {
            SegmentLaneFlags flags = new()
            {
                DrawFromStart = currentLanes.DrawFromStart,
                DrawToEnd = currentLanes.DrawToEnd,
                IsTheRevisionLane = currentLanes.IsTheRevisionLane
            };

            int startShift = currentLanes.CenterLane - currentLanes.StartLane;
            int endShift = currentLanes.EndLane - currentLanes.CenterLane;
            bool startIsDiagonal = Math.Abs(startShift) == 1;
            bool endIsDiagonal = Math.Abs(endShift) == 1;
            bool isBowOfDiagonals = startIsDiagonal && endIsDiagonal && -Math.Sign(startShift) == Math.Sign(endShift);
            int bowOffset = LaneWidth / 5;
            int junctionBowOffset = AppSettings.MergeGraphLanesHavingCommonParent.Value ? LaneLineWidth : bowOffset;
            flags.HorizontalOffset = isBowOfDiagonals ? -Math.Sign(startShift) * junctionBowOffset : 0;

            // Go perpendicularly through the center in order to avoid crossing independend nodes
            bool straightOneLaneDiagonals = AppSettings.StraightOneLaneDiagonals.Value;
            flags.DrawCenterToStartPerpendicularly = flags.DrawFromStart && (straightOneLaneDiagonals
                ? (startShift == 0 || (!startIsDiagonal && !flags.IsTheRevisionLane))
                : !startIsDiagonal);
            flags.DrawCenterToEndPerpendicularly = flags.DrawToEnd && (straightOneLaneDiagonals
                ? (endShift == 0 || (!endIsDiagonal && !flags.IsTheRevisionLane))
                : !endIsDiagonal);
            flags.DrawCenterPerpendicularly
                = isBowOfDiagonals
                //// lane shifted by one at end, not starting a diagonal over multiple lanes
                || (!straightOneLaneDiagonals
                    && (currentLanes.StartLane < 0 || startShift == 0)
                    && endIsDiagonal
                    && (nextLanes?.Value.EndLane is not >= 0 || endShift != nextLanes!.Value.EndLane - currentLanes.EndLane))
                //// lane shifted by one at start, not starting a diagonal over multiple lanes
                || (!straightOneLaneDiagonals
                    && (currentLanes.EndLane < 0 || endShift == 0)
                    && startIsDiagonal
                    && (previousLanes?.Value.StartLane is not >= 0 || startShift != currentLanes.StartLane - previousLanes!.Value.StartLane));
            flags.DrawCenter = flags.DrawCenterPerpendicularly
                || !flags.DrawFromStart
                || !flags.DrawToEnd
                || (!flags.DrawCenterToStartPerpendicularly && !flags.DrawCenterToEndPerpendicularly);

            // handle non-straight junctions
            if (currentLanes.EndLane < 0 && currentLanes.PrimaryEndLane >= 0 && startShift != 0)
            {
                endShift = currentLanes.PrimaryEndLane - currentLanes.CenterLane;
                bool sameDirection = Math.Sign(endShift) == Math.Sign(startShift);
                if (startIsDiagonal)
                {
                    if (straightOneLaneDiagonals && (!sameDirection || Math.Abs(endShift) > 1))
                    {
                        flags.DrawCenterToEndPerpendicularly = true;
                        flags.DrawCenter = false;
                        flags.HorizontalOffset = -Math.Sign(startShift) * (endShift == 0 || sameDirection ? LaneLineWidth / 3 : bowOffset);
                    }
                }
                else if (Math.Abs(endShift) == 1)
                {
                    // multi-lane crossing continued by a diagonal
                    flags.DrawCenterToStartPerpendicularly = false;
                    if (!sameDirection)
                    {
                        // bow
                        flags.HorizontalOffset = -Math.Sign(startShift) * LaneLineWidth * 2 / 3;
                    }
                }
                else if (straightOneLaneDiagonals)
                {
                    // multi-lane crossing continued by a straight or a multi-lane crossing
                    flags.DrawCenterToStartPerpendicularly = false;
                }
            }

            return flags;
        }

        private static void DrawSegmentCurvy(SegmentRenderer segmentRenderer, SegmentPointsInfo p, SegmentLanesInfo lanes)
        {
            if (lanes.DrawFromStart)
            {
                segmentRenderer.DrawTo(p.Start);
            }

            segmentRenderer.DrawTo(p.Center);

            if (lanes.DrawToEnd)
            {
                segmentRenderer.DrawTo(p.End);
            }
        }

        private static void DrawSegmentWithDiagonals(SegmentRenderer segmentRenderer,
            SegmentPointsInfo p,
            Lazy<SegmentLaneFlags> previousLaneFlags,
            SegmentLaneFlags currentLaneFlags,
            Lazy<SegmentLaneFlags> nextLaneFlags)
        {
            int halfPerpendicularHeight = segmentRenderer.RowHeight / 5;
            int diagonalLaneEndOffset = segmentRenderer.RowHeight / 20;

            if (currentLaneFlags.DrawFromStart)
            {
                SegmentLaneFlags previous = previousLaneFlags.Value;
                Debug.Assert(previous.DrawToEnd || AppSettings.MergeGraphLanesHavingCommonParent.Value, nameof(previous.DrawToEnd));
                int startX = p.Start.X + previous.HorizontalOffset;
                if (previous.DrawCenterToEndPerpendicularly)
                {
                    segmentRenderer.DrawTo(startX, p.Start.Y + halfPerpendicularHeight);
                }
                else if (previous.DrawCenter)
                {
                    // shift diagonal lane end
                    if (previous.IsTheRevisionLane && !previous.DrawCenterPerpendicularly && !previous.DrawFromStart)
                    {
                        segmentRenderer.DrawTo(startX, p.Start.Y + diagonalLaneEndOffset, toPerpendicularly: false);
                    }
                    else
                    {
                        segmentRenderer.DrawTo(startX, p.Start.Y, previous.DrawCenterPerpendicularly);
                    }
                }
                else
                {
                    segmentRenderer.DrawTo(startX, p.Start.Y - halfPerpendicularHeight);
                }
            }

            int centerX = p.Center.X + currentLaneFlags.HorizontalOffset;

            if (currentLaneFlags.DrawCenterToStartPerpendicularly)
            {
                segmentRenderer.DrawTo(centerX, p.Center.Y - halfPerpendicularHeight);
            }

            if (currentLaneFlags.DrawCenter)
            {
                // shift diagonal lane ends
                if (currentLaneFlags.IsTheRevisionLane && !currentLaneFlags.DrawCenterPerpendicularly && !currentLaneFlags.DrawToEnd)
                {
                    segmentRenderer.DrawTo(centerX, p.Center.Y - diagonalLaneEndOffset, toPerpendicularly: false);
                }
                else if (currentLaneFlags.IsTheRevisionLane && !currentLaneFlags.DrawCenterPerpendicularly && !currentLaneFlags.DrawFromStart)
                {
                    segmentRenderer.DrawTo(centerX, p.Center.Y + diagonalLaneEndOffset, toPerpendicularly: false);
                }
                else
                {
                    segmentRenderer.DrawTo(centerX, p.Center.Y, currentLaneFlags.DrawCenterPerpendicularly);
                }
            }

            if (currentLaneFlags.DrawCenterToEndPerpendicularly)
            {
                segmentRenderer.DrawTo(centerX, p.Center.Y + halfPerpendicularHeight);
            }

            if (currentLaneFlags.DrawToEnd)
            {
                SegmentLaneFlags next = nextLaneFlags.Value;
                Debug.Assert(next.DrawFromStart || AppSettings.MergeGraphLanesHavingCommonParent.Value, nameof(next.DrawFromStart));
                int endX = p.End.X + next.HorizontalOffset;
                if (next.DrawCenterToStartPerpendicularly)
                {
                    segmentRenderer.DrawTo(endX, p.End.Y - halfPerpendicularHeight);
                }
                else if (next.DrawCenter)
                {
                    // shift diagonal lane end
                    if (next.IsTheRevisionLane && !next.DrawCenterPerpendicularly && !next.DrawToEnd)
                    {
                        segmentRenderer.DrawTo(endX, p.End.Y - diagonalLaneEndOffset, toPerpendicularly: false);
                    }
                    else
                    {
                        segmentRenderer.DrawTo(endX, p.End.Y, next.DrawCenterPerpendicularly);
                    }
                }
                else
                {
                    segmentRenderer.DrawTo(endX, p.End.Y + halfPerpendicularHeight);
                }
            }
        }

        private static Brush GetBrushForLaneInfo(LaneInfo? laneInfo, bool isRelative, RevisionGraphDrawStyleEnum revisionGraphDrawStyle)
        {
            // laneInfo can be null for revisions without parents and children, especially when filtering, draw them gray, too
            if (laneInfo is null
                || (!isRelative && (revisionGraphDrawStyle is RevisionGraphDrawStyleEnum.DrawNonRelativesGray or RevisionGraphDrawStyleEnum.HighlightSelected)))
            {
                return RevisionGraphLaneColor.NonRelativeBrush;
            }

            return RevisionGraphLaneColor.GetBrushForLane(laneInfo.Color);
        }

        private static int GetLaneForRow(IRevisionGraphRow? row, RevisionGraphSegment revisionGraphRevision)
        {
            if (row is not null)
            {
                int lane = row.GetLaneForSegment(revisionGraphRevision).Index;
                if (lane >= 0)
                {
                    return lane;
                }
            }

            return _noLane;
        }
    }
}
