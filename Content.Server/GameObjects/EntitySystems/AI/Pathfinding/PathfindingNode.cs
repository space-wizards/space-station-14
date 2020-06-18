using System;
using System.Collections.Generic;
using Content.Server.GameObjects.EntitySystems.AI.Pathfinding;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.EntitySystems.Pathfinding
{
    public class PathfindingNode
    {
        // TODO: Add access ID here
        public PathfindingChunk ParentChunk => _parentChunk;
        private readonly PathfindingChunk _parentChunk;
        public TileRef TileRef { get; private set; }
        public List<int> CollisionLayers { get; }
        public int CollisionMask { get; private set; }
        public Dictionary<Direction, PathfindingNode> Neighbors => _neighbors;
        private Dictionary<Direction, PathfindingNode> _neighbors = new Dictionary<Direction, PathfindingNode>();

        public PathfindingNode(PathfindingChunk parent, TileRef tileRef, List<int> collisionLayers = null)
        {
            _parentChunk = parent;
            TileRef = tileRef;
            if (collisionLayers == null)
            {
                CollisionLayers = new List<int>();
            }
            else
            {
                CollisionLayers = collisionLayers;
            }
            GenerateMask();
        }

        public void AddNeighbor(Direction direction, PathfindingNode node)
        {
            _neighbors.Add(direction, node);
        }

        public void AddNeighbor(PathfindingNode node)
        {
            if (node.TileRef.GridIndex != TileRef.GridIndex)
            {
                throw new InvalidOperationException();
            }

            Direction direction;
            if (node.TileRef.X < TileRef.X)
            {
                if (node.TileRef.Y > TileRef.Y)
                {
                    direction = Direction.NorthWest;
                } else if (node.TileRef.Y < TileRef.Y)
                {
                    direction = Direction.SouthWest;
                }
                else
                {
                    direction = Direction.West;
                }
            }
            else if (node.TileRef.X > TileRef.X)
            {
                if (node.TileRef.Y > TileRef.Y)
                {
                    direction = Direction.NorthEast;
                } else if (node.TileRef.Y < TileRef.Y)
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
                if (node.TileRef.Y > TileRef.Y)
                {
                    direction = Direction.North;
                }
                else
                {
                    direction = Direction.South;
                }
            }

            if (_neighbors.ContainsKey(direction))
            {
                // Should we verify that they align?
                return;
            }

            _neighbors.Add(direction, node);
        }

        public PathfindingNode GetNeighbor(Direction direction)
        {
            _neighbors.TryGetValue(direction, out var node);
            return node;
        }

        public void UpdateTile(TileRef newTile)
        {
            TileRef = newTile;
        }

        public void AddCollisionLayer(int layer)
        {
            CollisionLayers.Add(layer);
            GenerateMask();
        }

        public void RemoveCollisionLayer(int layer)
        {
            CollisionLayers.Remove(layer);
            GenerateMask();
        }

        private void GenerateMask()
        {
            CollisionMask = 0x0;

            foreach (var layer in CollisionLayers)
            {
                CollisionMask |= layer;
            }
        }
    }
}
