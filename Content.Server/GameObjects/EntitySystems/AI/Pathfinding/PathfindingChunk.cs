using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Content.Server.GameObjects.EntitySystems.Pathfinding;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.EntitySystems.AI.Pathfinding
{
    public class PathfindingChunk
    {
        public GridId GridId { get; }

        public MapIndices Indices => _indices;
        private readonly MapIndices _indices;

        // Nodes per chunk row
        public static int ChunkSize => 16;
        public PathfindingNode[,] Nodes => _nodes;
        private PathfindingNode[,] _nodes = new PathfindingNode[ChunkSize,ChunkSize];

        public PathfindingChunk(GridId gridId, MapIndices indices)
        {
            GridId = gridId;
            _indices = indices;
        }

        public void Initialize(IMapGrid grid)
        {
            for (var x = 0; x < ChunkSize; x++)
            {
                for (var y = 0; y < ChunkSize; y++)
                {
                    var tileRef = grid.GetTileRef(new MapIndices(x + _indices.X, y + _indices.Y));
                    CreateNode(tileRef);
                }
            }
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
                    if (chunkGrid.TryGetValue(new MapIndices(neighborX, neighborY), out var neighbor))
                    {
                        yield return neighbor;
                    }
                }
            }
        }

        public bool InBounds(MapIndices mapIndices)
        {
            if (mapIndices.X < _indices.X || mapIndices.Y < _indices.Y) return false;
            if (mapIndices.X >= _indices.X + ChunkSize || mapIndices.Y >= _indices.Y + ChunkSize) return false;
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

        public void UpdateNode(TileRef tile)
        {
            var node = GetNode(tile);
            node.UpdateTile(tile);
        }

        private void CreateNode(TileRef tile, PathfindingChunk parent = null)
        {
            if (parent == null)
            {
                parent = this;
            }

            var node = new PathfindingNode(parent, tile);
            var offsetX = tile.X - Indices.X;
            var offsetY = tile.Y - Indices.Y;
            _nodes[offsetX, offsetY] = node;
        }
    }
}
