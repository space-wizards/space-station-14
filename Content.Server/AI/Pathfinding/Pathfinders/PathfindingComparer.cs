using System;
using System.Collections.Generic;

namespace Content.Server.AI.Pathfinding.Pathfinders
{
    public sealed class PathfindingComparer : IComparer<ValueTuple<float, PathfindingNode>>
    {
        public int Compare((float, PathfindingNode) x, (float, PathfindingNode) y)
        {
            return y.Item1.CompareTo(x.Item1);
        }
    }
}
