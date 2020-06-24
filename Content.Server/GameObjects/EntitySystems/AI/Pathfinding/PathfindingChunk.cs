using System;
using System.Collections.Generic;
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

        public void Initialize()
        {
            var grid = IoCManager.Resolve<IMapManager>().GetGrid(GridId);
            for (var x = 0; x < ChunkSize; x++)
            {
                for (var y = 0; y < ChunkSize; y++)
                {
                    var tileRef = grid.GetTileRef(new MapIndices(x + _indices.X, y + _indices.Y));
                    CreateNode(tileRef);
                }
            }

            RefreshNodeNeighbors();
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

        /// <summary>
        /// Updates all internal nodes with references to every other internal node
        /// </summary>
        private void RefreshNodeNeighbors()
        {
            for (var x = 0; x < ChunkSize; x++)
            {
                for (var y = 0; y < ChunkSize; y++)
                {
                    var node = _nodes[x, y];
                    // West
                    if (x != 0)
                    {
                        if (y != ChunkSize - 1)
                        {
                            node.AddNeighbor(Direction.NorthWest, _nodes[x - 1, y + 1]);
                        }
                        node.AddNeighbor(Direction.West, _nodes[x - 1, y]);
                        if (y != 0)
                        {
                            node.AddNeighbor(Direction.SouthWest, _nodes[x - 1, y - 1]);
                        }
                    }

                    // Same column
                    if (y != ChunkSize - 1)
                    {
                        node.AddNeighbor(Direction.North, _nodes[x, y + 1]);
                    }

                    if (y != 0)
                    {
                        node.AddNeighbor(Direction.South, _nodes[x, y - 1]);
                    }

                    // East
                    if (x != ChunkSize - 1)
                    {
                        if (y != ChunkSize - 1)
                        {
                            node.AddNeighbor(Direction.NorthEast, _nodes[x + 1, y + 1]);
                        }
                        node.AddNeighbor(Direction.East, _nodes[x + 1, y]);
                        if (y != 0)
                        {
                            node.AddNeighbor(Direction.SouthEast, _nodes[x + 1, y - 1]);
                        }
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

        public MapIndices RelativeIndices(MapIndices mapIndices)
        {
            // TODO: Get the relative (x, y) to our origin
            return;
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
