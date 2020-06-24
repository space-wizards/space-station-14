using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components.Access;
using Content.Server.GameObjects.Components.Doors;
using Content.Server.GameObjects.EntitySystems.AI.Pathfinding;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.EntitySystems.Pathfinding
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
        private readonly Dictionary<EntityUid, int> _blockedCollidables = new Dictionary<EntityUid, int>(0);

        public IReadOnlyCollection<EntityUid> PhysicsUids => _physicsUids;
        private readonly HashSet<EntityUid> _physicsUids = new HashSet<EntityUid>(0);

        /// <summary>
        /// The entities on this tile that require access to traverse
        /// </summary>
        /// We don't store the ICollection, at least for now, as we'd need to replicate the access code here
        public IReadOnlyCollection<AccessReader> AccessReaders => _accessReaders.Values;
        private readonly Dictionary<EntityUid, AccessReader> _accessReaders = new Dictionary<EntityUid, AccessReader>(0);

        public PathfindingNode(PathfindingChunk parent, TileRef tileRef)
        {
            _parentChunk = parent;
            TileRef = tileRef;
            GenerateMask();
        }

        public IEnumerable<PathfindingNode> GetNeighbors()
        {
            // TODO: Need to check if we need to get neighboring chunks
            for (var x = -1; x <= 1; x++)
            {
                for (var y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;
                    var indices = new MapIndices(TileRef.X + x, TileRef.Y + y);
                    if (ParentChunk.InBounds(indices))
                    {
                        yield return ParentChunk.Nodes[indi]
                    }
                    else
                    {
                        // Get the relevant chunk and then get the node on it
                    }
                }
            }
        }

        public void UpdateTile(TileRef newTile)
        {
            TileRef = newTile;
        }

        /// <summary>
        /// Call if this entity is relevant for the pathfinder
        /// </summary>
        /// <param name="entity"></param>
        /// TODO: These 2 methods currently don't account for a bunch of changes (e.g. airlock unpowered, wrenching, etc.)
        public void AddEntity(IEntity entity)
        {
            // If we're a door
            if (entity.HasComponent<AirlockComponent>() || entity.HasComponent<ServerDoorComponent>())
            {
                // If we need access to traverse this then add to readers, otherwise no point adding it (except for maybe tile costs in future)
                // TODO: Check for powered I think (also need an event for when it's depowered
                // AccessReader calls this whenever opening / closing but it can seem to get called multiple times
                // Which may or may not be intended?
                if (entity.TryGetComponent(out AccessReader accessReader) && !_accessReaders.ContainsKey(entity.Uid))
                {
                    _accessReaders.Add(entity.Uid, accessReader);
                }
                return;
            }
            
            if (entity.TryGetComponent(out CollidableComponent collidableComponent))
            {
                if (entity.TryGetComponent(out PhysicsComponent physicsComponent) && !physicsComponent.Anchored)
                {
                    _physicsUids.Add(entity.Uid);
                }
                else
                {
                    _blockedCollidables.TryAdd(entity.Uid, collidableComponent.CollisionLayer);
                    GenerateMask();
                }
            }
        }

        public void RemoveEntity(IEntity entity)
        {
            if (_accessReaders.ContainsKey(entity.Uid))
            {
                _accessReaders.Remove(entity.Uid);
                return;
            }

            if (entity.HasComponent<CollidableComponent>())
            {
                if (entity.TryGetComponent(out PhysicsComponent physicsComponent) && physicsComponent.Anchored)
                {
                    _blockedCollidables.Remove(entity.Uid);
                    GenerateMask();
                }
                else
                {
                    _physicsUids.Remove(entity.Uid);
                }
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
