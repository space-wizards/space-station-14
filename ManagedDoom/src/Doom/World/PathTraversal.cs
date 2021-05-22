//
// Copyright (C) 1993-1996 Id Software, Inc.
// Copyright (C) 2019-2020 Nobuaki Tanaka
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//



using System;

namespace ManagedDoom
{
    public sealed class PathTraversal
    {
        private World world;

        private Intercept[] intercepts;
        private int interceptCount;

        private bool earlyOut;

        private DivLine target;
        private DivLine trace;

        private Func<LineDef, bool> lineInterceptFunc;
        private Func<Mobj, bool> thingInterceptFunc;

        public PathTraversal(World world)
        {
            this.world = world;

            intercepts = new Intercept[256];
            for (var i = 0; i < intercepts.Length; i++)
            {
                intercepts[i] = new Intercept();
            }

            target = new DivLine();
            trace = new DivLine();

            lineInterceptFunc = AddLineIntercepts;
            thingInterceptFunc = AddThingIntercepts;
        }

        /// <summary>
        /// Looks for lines in the given block that intercept the given trace
        /// to add to the intercepts list.
        /// A line is crossed if its endpoints are on opposite sidesof the trace.
        /// Returns true if earlyOut and a solid line hit.
        /// </summary>
        private bool AddLineIntercepts(LineDef line)
        {
            int s1;
            int s2;

            // Avoid precision problems with two routines.
            if (trace.Dx > Fixed.FromInt(16) ||
                trace.Dy > Fixed.FromInt(16) ||
                trace.Dx < -Fixed.FromInt(16) ||
                trace.Dy < -Fixed.FromInt(16))
            {
                s1 = Geometry.PointOnDivLineSide(line.Vertex1.X, line.Vertex1.Y, trace);
                s2 = Geometry.PointOnDivLineSide(line.Vertex2.X, line.Vertex2.Y, trace);
            }
            else
            {
                s1 = Geometry.PointOnLineSide(trace.X, trace.Y, line);
                s2 = Geometry.PointOnLineSide(trace.X + trace.Dx, trace.Y + trace.Dy, line);
            }

            if (s1 == s2)
            {
                // Line isn't crossed.
                return true;
            }

            // Hit the line.
            target.MakeFrom(line);

            var frac = InterceptVector(trace, target);

            if (frac < Fixed.Zero)
            {
                // Behind source.
                return true;
            }

            // Try to early out the check.
            if (earlyOut && frac < Fixed.One && line.BackSector == null)
            {
                // Stop checking.
                return false;
            }

            intercepts[interceptCount].Make(frac, line);
            interceptCount++;

            // Continue.
            return true;
        }

        /// <summary>
        /// Looks for things that intercept the given trace.
        /// </summary>
        private bool AddThingIntercepts(Mobj thing)
        {
            var tracePositive = (trace.Dx.Data ^ trace.Dy.Data) > 0;

            Fixed x1;
            Fixed y1;
            Fixed x2;
            Fixed y2;

            // Check a corner to corner crossection for hit.
            if (tracePositive)
            {
                x1 = thing.X - thing.Radius;
                y1 = thing.Y + thing.Radius;

                x2 = thing.X + thing.Radius;
                y2 = thing.Y - thing.Radius;
            }
            else
            {
                x1 = thing.X - thing.Radius;
                y1 = thing.Y - thing.Radius;

                x2 = thing.X + thing.Radius;
                y2 = thing.Y + thing.Radius;
            }

            var s1 = Geometry.PointOnDivLineSide(x1, y1, trace);
            var s2 = Geometry.PointOnDivLineSide(x2, y2, trace);

            if (s1 == s2)
            {
                // Line isn't crossed.
                return true;
            }

            target.X = x1;
            target.Y = y1;
            target.Dx = x2 - x1;
            target.Dy = y2 - y1;

            var frac = InterceptVector(trace, target);

            if (frac < Fixed.Zero)
            {
                // Behind source.
                return true;
            }

            intercepts[interceptCount].Make(frac, thing);
            interceptCount++;

            // Keep going.
            return true;
        }

        /// <summary>
        /// Returns the fractional intercept point along the first divline.
        /// This is only called by the addthings and addlines traversers.
        /// </summary>
        private Fixed InterceptVector(DivLine v2, DivLine v1)
        {
            var den = (v1.Dy >> 8) * v2.Dx - (v1.Dx >> 8) * v2.Dy;

            if (den == Fixed.Zero)
            {
                return Fixed.Zero;
            }

            var num = ((v1.X - v2.X) >> 8) * v1.Dy + ((v2.Y - v1.Y) >> 8) * v1.Dx;

            var frac = num / den;

            return frac;
        }

        /// <summary>
        /// Returns true if the traverser function returns true for all lines.
        /// </summary>
        private bool TraverseIntercepts(Func<Intercept, bool> func, Fixed maxFrac)
        {
            var count = interceptCount;

            Intercept intercept = null;

            while (count-- > 0)
            {
                var dist = Fixed.MaxValue;
                for (var i = 0; i < interceptCount; i++)
                {
                    if (intercepts[i].Frac < dist)
                    {
                        dist = intercepts[i].Frac;
                        intercept = intercepts[i];
                    }
                }

                if (dist > maxFrac)
                {
                    // Checked everything in range.
                    return true;
                }

                if (!func(intercept))
                {
                    // Don't bother going farther.
                    return false;
                }

                intercept.Frac = Fixed.MaxValue;
            }

            // Everything was traversed.
            return true;
        }

        /// <summary>
        /// Traces a line from x1, y1 to x2, y2, calling the traverser function for each.
        /// Returns true if the traverser function returns true for all lines.
        /// </summary>
        public bool PathTraverse(Fixed x1, Fixed y1, Fixed x2, Fixed y2, PathTraverseFlags flags, Func<Intercept, bool> trav)
        {
            earlyOut = (flags & PathTraverseFlags.EarlyOut) != 0;

            var validCount = world.GetNewValidCount();

            var bm = world.Map.BlockMap;

            interceptCount = 0;

            if (((x1 - bm.OriginX).Data & (BlockMap.BlockSize.Data - 1)) == 0)
            {
                // Don't side exactly on a line.
                x1 += Fixed.One;
            }

            if (((y1 - bm.OriginY).Data & (BlockMap.BlockSize.Data - 1)) == 0)
            {
                // Don't side exactly on a line.
                y1 += Fixed.One;
            }

            trace.X = x1;
            trace.Y = y1;
            trace.Dx = x2 - x1;
            trace.Dy = y2 - y1;

            x1 -= bm.OriginX;
            y1 -= bm.OriginY;

            var blockX1 = x1.Data >> BlockMap.FracToBlockShift;
            var blockY1 = y1.Data >> BlockMap.FracToBlockShift;

            x2 -= bm.OriginX;
            y2 -= bm.OriginY;

            var blockX2 = x2.Data >> BlockMap.FracToBlockShift;
            var blockY2 = y2.Data >> BlockMap.FracToBlockShift;

            Fixed stepX;
            Fixed stepY;

            Fixed partial;

            int blockStepX;
            int blockStepY;

            if (blockX2 > blockX1)
            {
                blockStepX = 1;
                partial = new Fixed(Fixed.FracUnit - ((x1.Data >> BlockMap.BlockToFracShift) & (Fixed.FracUnit - 1)));
                stepY = (y2 - y1) / Fixed.Abs(x2 - x1);
            }
            else if (blockX2 < blockX1)
            {
                blockStepX = -1;
                partial = new Fixed((x1.Data >> BlockMap.BlockToFracShift) & (Fixed.FracUnit - 1));
                stepY = (y2 - y1) / Fixed.Abs(x2 - x1);
            }
            else
            {
                blockStepX = 0;
                partial = Fixed.One;
                stepY = Fixed.FromInt(256);
            }

            var interceptY = new Fixed(y1.Data >> BlockMap.BlockToFracShift) + (partial * stepY);


            if (blockY2 > blockY1)
            {
                blockStepY = 1;
                partial = new Fixed(Fixed.FracUnit - ((y1.Data >> BlockMap.BlockToFracShift) & (Fixed.FracUnit - 1)));
                stepX = (x2 - x1) / Fixed.Abs(y2 - y1);
            }
            else if (blockY2 < blockY1)
            {
                blockStepY = -1;
                partial = new Fixed((y1.Data >> BlockMap.BlockToFracShift) & (Fixed.FracUnit - 1));
                stepX = (x2 - x1) / Fixed.Abs(y2 - y1);
            }
            else
            {
                blockStepY = 0;
                partial = Fixed.One;
                stepX = Fixed.FromInt(256);
            }

            var interceptX = new Fixed(x1.Data >> BlockMap.BlockToFracShift) + (partial * stepX);

            // Step through map blocks.
            // Count is present to prevent a round off error from skipping the break.
            var bx = blockX1;
            var by = blockY1;

            for (var count = 0; count < 64; count++)
            {
                if ((flags & PathTraverseFlags.AddLines) != 0)
                {
                    if (!bm.IterateLines(bx, by, lineInterceptFunc, validCount))
                    {
                        // Early out.
                        return false;
                    }
                }

                if ((flags & PathTraverseFlags.AddThings) != 0)
                {
                    if (!bm.IterateThings(bx, by, thingInterceptFunc))
                    {
                        // Early out.
                        return false;
                    }
                }

                if (bx == blockX2 && by == blockY2)
                {
                    break;
                }

                if ((interceptY.ToIntFloor()) == by)
                {
                    interceptY += stepY;
                    bx += blockStepX;
                }
                else if ((interceptX.ToIntFloor()) == bx)
                {
                    interceptX += stepX;
                    by += blockStepY;
                }

            }

            // Go through the sorted list.
            return TraverseIntercepts(trav, Fixed.One);
        }

        public DivLine Trace => trace;
    }
}
