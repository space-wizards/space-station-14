using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Access;
using Content.Server.GameObjects.Components.Doors;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.EntitySystems.AI.Pathfinding
{
    public class PathfindingNode
    {
        public PathfindingChunk ParentChunk => _parentChunk;
        private readonly PathfindingChunk _parentChunk;

        public TileRef TileRef { get; private set; }

        /// <summary>
        /// Whenever there's a change in the collision layers we update the mask as the graph has more reads than writes
        /// </summary>
        public int BlockedCollisionMask { get; private set; }
        private readonly Dictionary<IEntity, int> _blockedCollidables = new Dictionary<IEntity, int>(0);

        public IReadOnlyDictionary<IEntity, int> PhysicsLayers => _physicsLayers;
        private readonly Dictionary<IEntity, int> _physicsLayers = new Dictionary<IEntity, int>(0);

        /// <summary>
        /// The entities on this tile that require access to traverse
        /// </summary>
        /// We don't store the ICollection, at least for now, as we'd need to replicate the access code here
        public IReadOnlyCollection<AccessReader> AccessReaders => _accessReaders.Values;
        private readonly Dictionary<IEntity, AccessReader> _accessReaders = new Dictionary<IEntity, AccessReader>(0);

        public PathfindingNode(PathfindingChunk parent, TileRef tileRef)
        {
            _parentChunk = parent;
            TileRef = tileRef;
            GenerateMask();
        }

        public static bool IsRelevant(IEntity entity, ICollidableComponent collidableComponent)
        {
            if (entity.Transform.GridID == GridId.Invalid ||
                (PathfindingSystem.TrackedCollisionLayers & collidableComponent.CollisionLayer) == 0)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Return our neighboring nodes (even across chunks)
        /// </summary>
        /// <returns></returns>
        public IEnumerable<PathfindingNode> GetNeighbors()
        {
            List<PathfindingChunk> neighborChunks = null;
            if (ParentChunk.OnEdge(this))
            {
                neighborChunks = ParentChunk.RelevantChunks(this).ToList();
            }

            for (var x = -1; x <= 1; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;
                    var indices = new Vector2i(TileRef.X + x, TileRef.Y + y);
                    if (ParentChunk.InBounds(indices))
                    {
                        var (relativeX, relativeY) = (indices.X - ParentChunk.Indices.X,
                            indices.Y - ParentChunk.Indices.Y);
                        yield return ParentChunk.Nodes[relativeX, relativeY];
                    }
                    else
                    {
                        DebugTools.AssertNotNull(neighborChunks);
                        // Get the relevant chunk and then get the node on it
                        foreach (var neighbor in neighborChunks)
                        {
                            // A lot of edge transitions are going to have a single neighboring chunk
                            // (given > 1 only affects corners)
                            // So we can just check the count to see if it's inbound
                            if (neighborChunks.Count > 0 && !neighbor.InBounds(indices)) continue;
                            var (relativeX, relativeY) = (indices.X - neighbor.Indices.X,
                                indices.Y - neighbor.Indices.Y);
                            yield return neighbor.Nodes[relativeX, relativeY];
                            break;
                        }
                    }
                }
            }
        }

        public PathfindingNode GetNeighbor(Direction direction)
        {
            var chunkXOffset = TileRef.X - ParentChunk.Indices.X;
            var chunkYOffset = TileRef.Y - ParentChunk.Indices.Y;
            Vector2i neighborVector2i;

            switch (direction)
            {
                case Direction.East:
                    if (!ParentChunk.OnEdge(this))
                    {
                        return ParentChunk.Nodes[chunkXOffset + 1, chunkYOffset];
                    }

                    neighborVector2i = new Vector2i(TileRef.X + 1, TileRef.Y);
                    foreach (var neighbor in ParentChunk.GetNeighbors())
                    {
                        if (neighbor.InBounds(neighborVector2i))
                        {
                            return neighbor.Nodes[neighborVector2i.X - neighbor.Indices.X,
                                neighborVector2i.Y - neighbor.Indices.Y];
                        }
                    }

                    return null;
                case Direction.NorthEast:
                    if (!ParentChunk.OnEdge(this))
                    {
                        return ParentChunk.Nodes[chunkXOffset + 1, chunkYOffset + 1];
                    }

                    neighborVector2i = new Vector2i(TileRef.X + 1, TileRef.Y + 1);
                    foreach (var neighbor in ParentChunk.GetNeighbors())
                    {
                        if (neighbor.InBounds(neighborVector2i))
                        {
                            return neighbor.Nodes[neighborVector2i.X - neighbor.Indices.X,
                                neighborVector2i.Y - neighbor.Indices.Y];
                        }
                    }

                    return null;
                case Direction.North:
                    if (!ParentChunk.OnEdge(this))
                    {
                        return ParentChunk.Nodes[chunkXOffset, chunkYOffset + 1];
                    }

                    neighborVector2i = new Vector2i(TileRef.X, TileRef.Y + 1);
                    foreach (var neighbor in ParentChunk.GetNeighbors())
                    {
                        if (neighbor.InBounds(neighborVector2i))
                        {
                            return neighbor.Nodes[neighborVector2i.X - neighbor.Indices.X,
                                neighborVector2i.Y - neighbor.Indices.Y];
                        }
                    }

                    return null;
                case Direction.NorthWest:
                    if (!ParentChunk.OnEdge(this))
                    {
                        return ParentChunk.Nodes[chunkXOffset - 1, chunkYOffset + 1];
                    }

                    neighborVector2i = new Vector2i(TileRef.X - 1, TileRef.Y + 1);
                    foreach (var neighbor in ParentChunk.GetNeighbors())
                    {
                        if (neighbor.InBounds(neighborVector2i))
                        {
                            return neighbor.Nodes[neighborVector2i.X - neighbor.Indices.X,
                                neighborVector2i.Y - neighbor.Indices.Y];
                        }
                    }

                    return null;
                case Direction.West:
                    if (!ParentChunk.OnEdge(this))
                    {
                        return ParentChunk.Nodes[chunkXOffset - 1, chunkYOffset];
                    }

                    neighborVector2i = new Vector2i(TileRef.X - 1, TileRef.Y);
                    foreach (var neighbor in ParentChunk.GetNeighbors())
                    {
                        if (neighbor.InBounds(neighborVector2i))
                        {
                            return neighbor.Nodes[neighborVector2i.X - neighbor.Indices.X,
                                neighborVector2i.Y - neighbor.Indices.Y];
                        }
                    }

                    return null;
                case Direction.SouthWest:
                    if (!ParentChunk.OnEdge(this))
                    {
                        return ParentChunk.Nodes[chunkXOffset - 1, chunkYOffset - 1];
                    }

                    neighborVector2i = new Vector2i(TileRef.X - 1, TileRef.Y - 1);
                    foreach (var neighbor in ParentChunk.GetNeighbors())
                    {
                        if (neighbor.InBounds(neighborVector2i))
                        {
                            return neighbor.Nodes[neighborVector2i.X - neighbor.Indices.X,
                                neighborVector2i.Y - neighbor.Indices.Y];
                        }
                    }

                    return null;
                case Direction.South:
                    if (!ParentChunk.OnEdge(this))
                    {
                        return ParentChunk.Nodes[chunkXOffset, chunkYOffset - 1];
                    }

                    neighborVector2i = new Vector2i(TileRef.X, TileRef.Y - 1);
                    foreach (var neighbor in ParentChunk.GetNeighbors())
                    {
                        if (neighbor.InBounds(neighborVector2i))
                        {
                            return neighbor.Nodes[neighborVector2i.X - neighbor.Indices.X,
                                neighborVector2i.Y - neighbor.Indices.Y];
                        }
                    }

                    return null;
                case Direction.SouthEast:
                    if (!ParentChunk.OnEdge(this))
                    {
                        return ParentChunk.Nodes[chunkXOffset + 1, chunkYOffset - 1];
                    }

                    neighborVector2i = new Vector2i(TileRef.X + 1, TileRef.Y - 1);
                    foreach (var neighbor in ParentChunk.GetNeighbors())
                    {
                        if (neighbor.InBounds(neighborVector2i))
                        {
                            return neighbor.Nodes[neighborVector2i.X - neighbor.Indices.X,
                                neighborVector2i.Y - neighbor.Indices.Y];
                        }
                    }

                    return null;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        public void UpdateTile(TileRef newTile)
        {
            TileRef = newTile;
            ParentChunk.Dirty();
        }

        /// <summary>
        /// Call if this entity is relevant for the pathfinder
        /// </summary>
        /// <param name="entity"></param>
        /// TODO: These 2 methods currently don't account for a bunch of changes (e.g. airlock unpowered, wrenching, etc.)
        /// TODO: Could probably optimise this slightly more.
        public void AddEntity(IEntity entity, ICollidableComponent collidableComponent)
        {
            // If we're a door
            if (entity.HasComponent<AirlockComponent>() || entity.HasComponent<ServerDoorComponent>())
            {
                // If we need access to traverse this then add to readers, otherwise no point adding it (except for maybe tile costs in future)
                // TODO: Check for powered I think (also need an event for when it's depowered
                // AccessReader calls this whenever opening / closing but it can seem to get called multiple times
                // Which may or may not be intended?
                if (entity.TryGetComponent(out AccessReader accessReader) && !_accessReaders.ContainsKey(entity))
                {
                    _accessReaders.Add(entity, accessReader);
                    ParentChunk.Dirty();
                }
                return;
            }

            DebugTools.Assert((PathfindingSystem.TrackedCollisionLayers & collidableComponent.CollisionLayer) != 0);

            if (!collidableComponent.Anchored)
            {
                _physicsLayers.Add(entity, collidableComponent.CollisionLayer);
            }
            else
            {
                _blockedCollidables.Add(entity, collidableComponent.CollisionLayer);
                GenerateMask();
                ParentChunk.Dirty();
            }
        }

        /// <summary>
        /// Remove the entity from this node.
        /// Will check each category and remove it from the applicable one
        /// </summary>
        /// <param name="entity"></param>
        public void RemoveEntity(IEntity entity)
        {
            // There's no guarantee that the entity isn't deleted
            // 90% of updates are probably entities moving around
            // Entity can't be under multiple categories so just checking each once is fine.
            if (_physicsLayers.ContainsKey(entity))
            {
                _physicsLayers.Remove(entity);
            }
            else if (_accessReaders.ContainsKey(entity))
            {
                _accessReaders.Remove(entity);
                ParentChunk.Dirty();
            }
            else if (_blockedCollidables.ContainsKey(entity))
            {
                _blockedCollidables.Remove(entity);
                GenerateMask();
                ParentChunk.Dirty();
            }
        }

        private void GenerateMask()
        {
            BlockedCollisionMask = 0x0;

            foreach (var layer in _blockedCollidables.Values)
            {
                BlockedCollisionMask |= layer;
            }
        }
    }
}
