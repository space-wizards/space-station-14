using System;
using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems.Pathfinding;
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
        public Dictionary<Direction, PathfindingChunk> Neighbors { get; } = new Dictionary<Direction, PathfindingChunk>(8);

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

        /// <summary>
        /// This will work both ways
        /// </summary>
        /// <param name="chunk"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void AddNeighbor(PathfindingChunk chunk)
        {
            if (chunk == this) return;
            if (Neighbors.ContainsValue(chunk))
            {
                return;
            }

            Direction direction;
            if (chunk.Indices.X < _indices.X)
            {
                if (chunk.Indices.Y > _indices.Y)
                {
                    direction = Direction.NorthWest;
                } else if (chunk.Indices.Y < _indices.Y)
                {
                    direction = Direction.SouthWest;
                }
                else
                {
                    direction = Direction.West;
                }
            }
            else if (chunk.Indices.X > _indices.X)
            {
                if (chunk.Indices.Y > _indices.Y)
                {
                    direction = Direction.NorthEast;
                } else if (chunk.Indices.Y < _indices.Y)
                {
                    direction = Direction.SouthEast;
                }
                else
                {
                    direction = Direction.East;
                }
            }
            else
            {
                if (chunk.Indices.Y > _indices.Y)
                {
                    direction = Direction.North;
                } else if (chunk.Indices.Y < _indices.Y)
                {
                    direction = Direction.South;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }

            Neighbors.TryAdd(direction, chunk);

            foreach (var node in GetBorderNodes(direction))
            {
                foreach (var counter in chunk.GetCounterpartNodes(direction))
                {
                    var xDiff = node.TileRef.X - counter.TileRef.X;
                    var yDiff = node.TileRef.Y - counter.TileRef.Y;

                    if (Math.Abs(xDiff) <= 1 && Math.Abs(yDiff) <= 1)
                    {
                        node.AddNeighbor(counter);
                        counter.AddNeighbor(node);
                    }
                }
            }

            chunk.Neighbors.TryAdd(OppositeDirection(direction), this);

            if (Neighbors.Count > 8)
            {
                throw new InvalidOperationException();
            }
        }

        private Direction OppositeDirection(Direction direction)
        {
            return (Direction) (((int) direction + 4) % 8);
        }

        // TODO I was too tired to think of an easier system. Could probably just google an array wraparound
        private IEnumerable<PathfindingNode> GetCounterpartNodes(Direction direction)
        {
            switch (direction)
            {
                case Direction.West:
                    for (var i = 0; i < ChunkSize; i++)
                    {
                        yield return _nodes[ChunkSize - 1, i];
                    }
                    break;
                case Direction.SouthWest:
                    yield return _nodes[ChunkSize - 1, ChunkSize - 1];
                    break;
                case Direction.South:
                    for (var i = 0; i < ChunkSize; i++)
                    {
                        yield return _nodes[i, ChunkSize - 1];
                    }
                    break;
                case Direction.SouthEast:
                    yield return _nodes[0, ChunkSize - 1];
                    break;
                case Direction.East:
                    for (var i = 0; i < ChunkSize; i++)
                    {
                        yield return _nodes[0, i];
                    }
                    break;
                case Direction.NorthEast:
                    yield return _nodes[0, 0];
                    break;
                case Direction.North:
                    for (var i = 0; i < ChunkSize; i++)
                    {
                        yield return _nodes[i, 0];
                    }
                    break;
                case Direction.NorthWest:
                    yield return _nodes[ChunkSize - 1, 0];
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        public IEnumerable<PathfindingNode> GetBorderNodes(Direction direction)
        {
            switch (direction)
            {
                case Direction.East:
                    for (var i = 0; i < ChunkSize; i++)
                    {
                        yield return _nodes[ChunkSize - 1, i];
                    }
                    break;
                case Direction.NorthEast:
                    yield return _nodes[ChunkSize - 1, ChunkSize - 1];
                    break;
                case Direction.North:
                    for (var i = 0; i < ChunkSize; i++)
                    {
                        yield return _nodes[i, ChunkSize - 1];
                    }
                    break;
                case Direction.NorthWest:
                    yield return _nodes[0, ChunkSize - 1];
                    break;
                case Direction.West:
                    for (var i = 0; i < ChunkSize; i++)
                    {
                        yield return _nodes[0, i];
                    }
                    break;
                case Direction.SouthWest:
                    yield return _nodes[0, 0];
                    break;
                case Direction.South:
                    for (var i = 0; i < ChunkSize; i++)
                    {
                        yield return _nodes[i, 0];
                    }
                    break;
                case Direction.SouthEast:
                    yield return _nodes[ChunkSize - 1, 0];
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        public bool InBounds(TileRef tile)
        {
            if (tile.X < _indices.X || tile.Y < _indices.Y) return false;
            if (tile.X >= _indices.X + ChunkSize || tile.Y >= _indices.Y + ChunkSize) return false;
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
