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
	public sealed class VisibilityCheck
	{
		private World world;

		// Eye z of looker.
		private Fixed sightZStart;
		private Fixed bottomSlope;
		private Fixed topSlope;

		// From looker to target.
		private DivLine trace;
		private Fixed targetX;
		private Fixed targetY;

		private DivLine occluder;

		public VisibilityCheck(World world)
		{
			this.world = world;

			trace = new DivLine();

			occluder = new DivLine();
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
		/// Returns true if strace crosses the given subsector successfully.
		/// </summary>
		private bool CrossSubsector(int subsectorNumber, int validCount)
		{
			var map = world.Map;
			var subsector = map.Subsectors[subsectorNumber];
			var count = subsector.SegCount;

			// Check lines.
			for (var i = 0; i < count; i++)
			{
				var seg = map.Segs[subsector.FirstSeg + i];
				var line = seg.LineDef;

				// Allready checked other side?
				if (line.ValidCount == validCount)
				{
					continue;
				}

				line.ValidCount = validCount;

				var v1 = line.Vertex1;
				var v2 = line.Vertex2;
				var s1 = Geometry.DivLineSide(v1.X, v1.Y, trace);
				var s2 = Geometry.DivLineSide(v2.X, v2.Y, trace);

				// Line isn't crossed?
				if (s1 == s2)
				{
					continue;
				}

				occluder.MakeFrom(line);
				s1 = Geometry.DivLineSide(trace.X, trace.Y, occluder);
				s2 = Geometry.DivLineSide(targetX, targetY, occluder);

				// Line isn't crossed?
				if (s1 == s2)
				{
					continue;
				}

				// The check below is imported from Chocolate Doom to
				// avoid crash due to two-sided lines with no backsector.
				if (line.BackSector == null)
				{
					return false;
				}

				// Stop because it is not two sided anyway.
				// Might do this after updating validcount?
				if ((line.Flags & LineFlags.TwoSided) == 0)
				{
					return false;
				}

				// Crosses a two sided line.
				var front = seg.FrontSector;
				var back = seg.BackSector;

				// No wall to block sight with?
				if (front.FloorHeight == back.FloorHeight &&
					front.CeilingHeight == back.CeilingHeight)
				{
					continue;
				}

				// Possible occluder because of ceiling height differences.
				Fixed openTop;
				if (front.CeilingHeight < back.CeilingHeight)
				{
					openTop = front.CeilingHeight;
				}
				else
				{
					openTop = back.CeilingHeight;
				}

				// Because of ceiling height differences.
				Fixed openBottom;
				if (front.FloorHeight > back.FloorHeight)
				{
					openBottom = front.FloorHeight;
				}
				else
				{
					openBottom = back.FloorHeight;
				}

				// Quick test for totally closed doors.
				if (openBottom >= openTop)
				{
					// Stop.
					return false;
				}

				var frac = InterceptVector(trace, occluder);

				if (front.FloorHeight != back.FloorHeight)
				{
					var slope = (openBottom - sightZStart) / frac;
					if (slope > bottomSlope)
					{
						bottomSlope = slope;
					}
				}

				if (front.CeilingHeight != back.CeilingHeight)
				{
					var slope = (openTop - sightZStart) / frac;
					if (slope < topSlope)
					{
						topSlope = slope;
					}
				}

				if (topSlope <= bottomSlope)
				{
					// Stop.
					return false;
				}
			}

			// Passed the subsector ok.
			return true;
		}

		/// <summary>
		/// Returns true if strace crosses the given node successfully.
		/// </summary>
		private bool CrossBspNode(int nodeNumber, int validCount)
		{
			if (Node.IsSubsector(nodeNumber))
			{
				if (nodeNumber == -1)
				{
					return CrossSubsector(0, validCount);
				}
				else
				{
					return CrossSubsector(Node.GetSubsector(nodeNumber), validCount);
				}
			}

			var node = world.Map.Nodes[nodeNumber];

			// Decide which side the start point is on.
			var side = Geometry.DivLineSide(trace.X, trace.Y, node);
			if (side == 2)
			{
				// An "on" should cross both sides.
				side = 0;
			}

			// cross the starting side
			if (!CrossBspNode(node.Children[side], validCount))
			{
				return false;
			}

			// The partition plane is crossed here.
			if (side == Geometry.DivLineSide(targetX, targetY, node))
			{
				// The line doesn't touch the other side.
				return true;
			}

			// Cross the ending side.
			return CrossBspNode(node.Children[side ^ 1], validCount);
		}

		/// <summary>
		/// Returns true if a straight line between the looker and target is unobstructed.
		/// </summary>
		public bool CheckSight(Mobj looker, Mobj target)
		{
			var map = world.Map;

			// First check for trivial rejection.
			// Check in REJECT table.
			if (map.Reject.Check(looker.Subsector.Sector, target.Subsector.Sector))
			{
				// Can't possibly be connected.
				return false;
			}

			// An unobstructed LOS is possible.
			// Now look from eyes of t1 to any part of t2.

			sightZStart = looker.Z + looker.Height - (looker.Height >> 2);
			topSlope = (target.Z + target.Height) - sightZStart;
			bottomSlope = (target.Z) - sightZStart;

			trace.X = looker.X;
			trace.Y = looker.Y;
			trace.Dx = target.X - looker.X;
			trace.Dy = target.Y - looker.Y;

			targetX = target.X;
			targetY = target.Y;

			// The head node is the last node output.
			return CrossBspNode(map.Nodes.Length - 1, world.GetNewValidCount());
		}
	}
}
