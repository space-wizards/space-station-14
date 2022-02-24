using System;
using System.Collections.Generic;

namespace Content.Server.AI.Pathfinding.Accessible
{
    /// <summary>
    /// A group of homogenous PathfindingNodes inside a single chunk
    /// </summary>
    /// Makes the graph smaller and quicker to traverse
    public sealed class PathfindingRegion : IEquatable<PathfindingRegion>
    {
        /// <summary>
        /// Bottom-left reference node of the region
        /// </summary>
        public PathfindingNode OriginNode { get; }

        // The shape may be anything within the bounds of a chunk, this is just a quick way to do a bounds-check

        /// <summary>
        /// Maximum width of the nodes
        /// </summary>
        public int Height { get; private set; } = 1;

        /// <summary>
        /// Maximum width of the nodes
        /// </summary>
        public int Width { get; private set; } = 1;

        public PathfindingChunk ParentChunk => OriginNode.ParentChunk;
        public HashSet<PathfindingRegion> Neighbors { get; } = new();

        public bool IsDoor { get; }
        public HashSet<PathfindingNode> Nodes => _nodes;
        private readonly HashSet<PathfindingNode> _nodes;

        public bool Deleted { get; private set; }

        public PathfindingRegion(PathfindingNode originNode, HashSet<PathfindingNode> nodes, bool isDoor = false)
        {
            OriginNode = originNode;
            _nodes = nodes;
            IsDoor = isDoor;
        }

        public void Shutdown()
        {
            // Tell our neighbors we no longer exist ;-/
            var neighbors = new List<PathfindingRegion>(Neighbors);

            for (var i = 0; i < neighbors.Count; i++)
            {
                var neighbor = neighbors[i];
                neighbor.Neighbors.Remove(this);
            }

            _nodes.Clear();
            Neighbors.Clear();

            Deleted = true;
        }

        /// <summary>
        /// Roughly how far away another region is by nearest node
        /// </summary>
        /// <param name="otherRegion"></param>
        /// <returns></returns>
        public float Distance(PathfindingRegion otherRegion)
        {
            // JANK
            var xDistance = otherRegion.OriginNode.TileRef.X - OriginNode.TileRef.X;
            var yDistance = otherRegion.OriginNode.TileRef.Y - OriginNode.TileRef.Y;

            if (xDistance > 0)
            {
                xDistance -= Width;
            }
            else if (xDistance < 0)
            {
                xDistance = Math.Abs(xDistance + otherRegion.Width);
            }

            if (yDistance > 0)
            {
                yDistance -= Height;
            }
            else if (yDistance < 0)
            {
                yDistance = Math.Abs(yDistance + otherRegion.Height);
            }

            return PathfindingHelpers.OctileDistance(xDistance, yDistance);
        }

        /// <summary>
        /// Can the given args can traverse this region?
        /// </summary>
        /// <param name="reachableArgs"></param>
        /// <returns></returns>
        public bool RegionTraversable(ReachableArgs reachableArgs)
        {
            // The assumption is that all nodes in a region have the same pathfinding traits
            // As such we can just use the origin node for checking.
            return PathfindingHelpers.Traversable(reachableArgs.CollisionMask, reachableArgs.Access,
                OriginNode);
        }

        public void Add(PathfindingNode node)
        {
            var xWidth = Math.Abs(node.TileRef.X - OriginNode.TileRef.X);
            var yHeight = Math.Abs(node.TileRef.Y - OriginNode.TileRef.Y);

            if (xWidth > Width)
            {
                Width = xWidth;
            }

            if (yHeight > Height)
            {
                Height = yHeight;
            }

            _nodes.Add(node);
        }

        // HashSet wasn't working correctly so uhh we got this.
        public bool Equals(PathfindingRegion? other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (_nodes.Count != other.Nodes.Count) return false;
            if (Deleted != other.Deleted) return false;
            if (OriginNode != other.OriginNode) return false;
            return true;
        }

        public override int GetHashCode()
        {
            return OriginNode.GetHashCode();
        }
    }
}
