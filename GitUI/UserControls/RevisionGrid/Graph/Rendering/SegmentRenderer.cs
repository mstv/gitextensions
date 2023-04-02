﻿using System.Drawing.Drawing2D;
using GitCommands;

namespace GitUI.UserControls.RevisionGrid.Graph.Rendering
{
    internal ref struct SegmentRenderer
    {
        public readonly int RowHeight => _context.CellSize.Height;

        private readonly Context _context;

        private bool _fromPerpendicularly = true;
        private Point? _fromPoint = null;

        public SegmentRenderer(in Context context)
        {
            _context = context;
        }

        public void DrawTo(in int x, in int y, in bool toPerpendicularly = true)
            => DrawTo(new Point(x, y), toPerpendicularly);

        public void DrawTo(in Point toPoint, in bool toPerpendicularly = true)
        {
            try
            {
                if (_fromPoint is null)
                {
                    return;
                }

                DrawTo(_fromPoint.Value, toPoint, _fromPerpendicularly, toPerpendicularly, _context);
            }
            finally
            {
                _fromPoint = toPoint;
                _fromPerpendicularly = toPerpendicularly;
            }
        }

        private static void DrawTo(in Point fromPoint, in Point toPoint, in bool fromPerpendicularly, in bool toPerpendicularly, in Context context)
        {
            Graphics g = context.G;
            Pen pen = context.Pen;

            if (AppSettings.DebugGraphPoints.Value)
            {
                if (fromPoint.X == toPoint.X)
                {
                    return;
                }

                g.SmoothingMode = SmoothingMode.None;
                DrawLine(new Point(fromPoint.X, fromPoint.Y - 1), new Point(fromPoint.X, fromPoint.Y + 1));
                DrawLine(new Point(toPoint.X, toPoint.Y - 1), new Point(toPoint.X, toPoint.Y + 1));
                return;
            }

            if (fromPoint.X == toPoint.X)
            {
                if (AppSettings.DebugGraphCurves.Value)
                {
                    return;
                }

                // direct line without anti-aliasing
                g.SmoothingMode = SmoothingMode.None;
                DrawLine(fromPoint, toPoint);
                return;
            }

            // Anti-aliasing with PixelOffsetMode.HighQuality introduces an offset of ~1/8 px - compensate it.
            g.SmoothingMode = SmoothingMode.AntiAlias;
            const float antiAliasOffset = -1f / 8f;
            PointF e0 = new(antiAliasOffset + fromPoint.X, fromPoint.Y);
            PointF e1 = new(antiAliasOffset + toPoint.X, toPoint.Y);

            int height = toPoint.Y - fromPoint.Y;
            int width = toPoint.X - fromPoint.X;
            bool singleLane = Math.Abs(width) <= context.CellSize.Width;
            Size cellShift = new(Math.Sign(width) * context.CellSize.Width, context.CellSize.Height);

            if (!fromPerpendicularly && !toPerpendicularly && singleLane)
            {
                if (AppSettings.DebugGraphCurves.Value)
                {
                    return;
                }

                // Direct line with anti-aliasing
                DrawLine(e0, e1);
            }
            else
            {
                // Control points for Bezier curve
                PointF c0 = e0;
                PointF c1 = e1;

                const float diagonalFractionCurve = 1f / 4f;
                const float perpendicularFraction = diagonalFractionCurve;
                float perpendicularOffset = perpendicularFraction * cellShift.Height;

                if (fromPerpendicularly && toPerpendicularly)
                {
                    c0.Y += perpendicularOffset;
                    c1.Y -= perpendicularOffset;

                    PointF mid = new(0.5f * (e0.X + e1.X), 0.5f * (e0.Y + e1.Y));
                    SizeF shift = diagonalFractionCurve * cellShift;
                    DrawBezier(e0, c0, mid - shift, mid);
                    DrawBezier(e1, c1, mid + shift, mid);
                    return;
                }
                else
                {
                    // Is the end of a diagonal
                    if (singleLane)
                    {
                        float diagonalFractionStraight = height < cellShift.Height ? 2f / 5f : 1f / 2f;

                        if (fromPerpendicularly)
                        {
                            MoveDrawDiagonallyFrom(ref e1, out _, -diagonalFractionStraight);

                            // Prepare remaining curve
                            c1 = e1 - (diagonalFractionCurve * cellShift);
                            c0.Y += perpendicularOffset;
                        }
                        else
                        {
                            MoveDrawDiagonallyFrom(ref e0, out _, +diagonalFractionStraight);

                            // Prepare remaining curve
                            c0 = e0 + (diagonalFractionCurve * cellShift);
                            c1.Y -= perpendicularOffset;
                        }
                    }

                    // Is a multi-lane crossing
                    else
                    {
                        float diagonalFractionStraight = 1f / 6f;

                        if (fromPerpendicularly)
                        {
                            c0.Y += perpendicularOffset;
                        }
                        else
                        {
                            MoveDrawDiagonallyFrom(ref e0, out c0, +diagonalFractionStraight);
                        }

                        if (toPerpendicularly)
                        {
                            c1.Y -= perpendicularOffset;
                        }
                        else
                        {
                            MoveDrawDiagonallyFrom(ref e1, out c1, -diagonalFractionStraight);
                        }
                    }
                }

                DrawBezier(e0, c0, c1, e1);

                return;
            }

            void DrawBezier(in PointF e0, in PointF c0, in PointF c1, in PointF e1)
            {
                if (AppSettings.DebugGraphCurves.Value)
                {
                    DrawLine(e0, new(e0.X, e0.Y + 1));
                    DrawLine(c0, new(c0.X, c0.Y + 1));
                    DrawLine(c1, new(c1.X, c1.Y + 1));
                    DrawLine(e1, new(e1.X, e1.Y + 1));
                }
                else
                {
                    g.DrawBezier(pen, e0, c0, c1, e1);
                }
            }

            void DrawLine(in PointF from, in PointF to)
            {
                g.DrawLine(pen, from, to);
            }

            void MoveDrawDiagonallyFrom(ref PointF start, out PointF bezierCenter, in float fractionOfCell)
            {
                SizeF shift = fractionOfCell * cellShift;
                PointF end = start + shift;
                if (!AppSettings.DebugGraphCurves.Value)
                {
                    g.DrawLine(pen, start, end);
                }

                start = end;
                bezierCenter = end + shift;
            }
        }
    }
}
