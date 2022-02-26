using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Server.AI.Pathfinding
{
    public sealed class PathfindingChunkUpdateMessage : EntityEventArgs
    {
        public PathfindingChunk Chunk { get; }

        public PathfindingChunkUpdateMessage(PathfindingChunk chunk)
        {
            Chunk = chunk;
        }
    }

    public sealed class PathfindingChunk
    {
        public TimeSpan LastUpdate { get; private set; }
        public GridId GridId { get; }

        public Vector2i Indices => _indices;
        private readonly Vector2i _indices;

        // Nodes per chunk row
        public static int ChunkSize => 8;
        public PathfindingNode[,] Nodes => _nodes;
        private readonly PathfindingNode[,] _nodes = new PathfindingNode[ChunkSize,ChunkSize];

        public PathfindingChunk(GridId gridId, Vector2i indices)
        {
            GridId = gridId;
            _indices = indices;
        }

        public void Initialize(IMapGrid mapGrid)
        {
            for (var x = 0; x < ChunkSize; x++)
            {
                for (var y = 0; y < ChunkSize; y++)
                {
                    var tileRef = mapGrid.GetTileRef(new Vector2i(x + _indices.X, y + _indices.Y));
                    CreateNode(tileRef);
                }
            }

            Dirty();
        }

        /// <summary>
        /// Only called when blockers change (i.e. un-anchored physics objects don't trigger)
        /// </summary>
        public void Dirty()
        {
            LastUpdate = IoCManager.Resolve<IGameTiming>().CurTime;
            IoCManager.Resolve<IEntityManager>().EventBus
                .RaiseEvent(EventSource.Local, new PathfindingChunkUpdateMessage(this));
        }

        public IEnumerable<PathfindingChunk> GetNeighbors()
        {
            var pathfindingSystem = EntitySystem.Get<PathfindingSystem>();
            var chunkGrid = pathfindingSystem.Graph[GridId];

            for (var x = -1; x <= 1; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;
                    var (neighborX, neighborY) = (_indices.X + ChunkSize * x, _indices.Y + ChunkSize * y);
                    if (chunkGrid.TryGetValue(new Vector2i(neighborX, neighborY), out var neighbor))
                    {
                        yield return neighbor;
                    }
                }
            }
        }

        public bool InBounds(Vector2i vector)
        {
            if (vector.X < _indices.X || vector.Y < _indices.Y) return false;
            if (vector.X >= _indices.X + ChunkSize || vector.Y >= _indices.Y + ChunkSize) return false;
            return true;
        }

        /// <summary>
        /// Returns true if the tile is on the outer edge
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool OnEdge(PathfindingNode node)
        {
            if (node.TileRef.X == _indices.X) return true;
            if (node.TileRef.Y == _indices.Y) return true;
            if (node.TileRef.X == _indices.X  + ChunkSize - 1) return true;
            if (node.TileRef.Y == _indices.Y  + ChunkSize - 1) return true;
            return false;
        }

        /// <summary>
        /// Gets our neighbors that are relevant for the node to retrieve its own neighbors
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public IEnumerable<PathfindingChunk> RelevantChunks(PathfindingNode node)
        {
            var relevantDirections = GetEdges(node).ToList();

            foreach (var chunk in GetNeighbors())
            {
                var chunkDirection = PathfindingHelpers.RelativeDirection(chunk, this);
                if (relevantDirections.Contains(chunkDirection))
                {
                    yield return chunk;
                }
            }
        }

        private IEnumerable<Direction> GetEdges(PathfindingNode node)
        {
            // West Edge
            if (node.TileRef.X == _indices.X)
            {
                yield return Direction.West;
                if (node.TileRef.Y == _indices.Y)
                {
                    yield return Direction.SouthWest;
                    yield return Direction.South;
                } else if (node.TileRef.Y == _indices.Y + ChunkSize - 1)
                {
                    yield return Direction.NorthWest;
                    yield return Direction.North;
                }

                yield break;
            }
            // East edge
            if (node.TileRef.X == _indices.X + ChunkSize - 1)
            {
                yield return Direction.East;
                if (node.TileRef.Y == _indices.Y)
                {
                    yield return Direction.SouthEast;
                    yield return Direction.South;
                } else if (node.TileRef.Y == _indices.Y + ChunkSize - 1)
                {
                    yield return Direction.NorthEast;
                    yield return Direction.North;
                }

                yield break;

            }
            // South edge
            if (node.TileRef.Y == _indices.Y)
            {
                yield return Direction.South;
                // Given we already checked south-west and south-east above shouldn't need any more
            }
            // North edge
            if (node.TileRef.Y == _indices.Y + ChunkSize - 1)
            {
                yield return Direction.North;
            }
        }

        public PathfindingNode GetNode(TileRef tile)
        {
            var chunkX = tile.X - _indices.X;
            var chunkY = tile.Y - _indices.Y;

            return _nodes[chunkX, chunkY];
        }

        private void CreateNode(TileRef tile, PathfindingChunk? parent = null)
        {
            parent ??= this;

            var node = new PathfindingNode(parent, tile);
            var offsetX = tile.X - Indices.X;
            var offsetY = tile.Y - Indices.Y;
            _nodes[offsetX, offsetY] = node;
        }
    }
}
