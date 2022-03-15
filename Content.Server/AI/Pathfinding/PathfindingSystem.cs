using System;
using System.Collections.Generic;
using System.Threading;
using Content.Server.Access;
using Content.Server.AI.Pathfinding.Pathfinders;
using Content.Server.CPUJob.JobQueues;
using Content.Server.CPUJob.JobQueues.Queues;
using Content.Shared.Access.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Utility;

namespace Content.Server.AI.Pathfinding
{
    /*
    // TODO: IMO use rectangular symmetry reduction on the nodes with collision at all. (currently planned to be implemented via AiReachableSystem and expanded later).
    alternatively store all rooms and have an alternative graph for humanoid mobs (same collision mask, needs access etc). You could also just path from room to room as needed.
    // TODO: Longer term -> Handle collision layer changes?
    TODO: Handle container entities so they're not tracked.
    */
    /// <summary>
    /// This system handles pathfinding graph updates as well as dispatches to the pathfinder
    /// (90% of what it's doing is graph updates so not much point splitting the 2 roles)
    /// </summary>
    public class PathfindingSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly AccessReaderSystem _accessReader = default!;

        public IReadOnlyDictionary<GridId, Dictionary<Vector2i, PathfindingChunk>> Graph => _graph;
        private readonly Dictionary<GridId, Dictionary<Vector2i, PathfindingChunk>> _graph = new();

        private readonly PathfindingJobQueue _pathfindingQueue = new();

        // Queued pathfinding graph updates
        private readonly Queue<CollisionChangeMessage> _collidableUpdateQueue = new();
        private readonly Queue<MoveEvent> _moveUpdateQueue = new();
        private readonly Queue<AccessReaderChangeMessage> _accessReaderUpdateQueue = new();
        private readonly Queue<TileRef> _tileUpdateQueue = new();

        // Need to store previously known entity positions for collidables for when they move
        private readonly Dictionary<EntityUid, PathfindingNode> _lastKnownPositions = new();

        public const int TrackedCollisionLayers = (int)
            (CollisionGroup.Impassable |
             CollisionGroup.MobImpassable |
             CollisionGroup.SmallImpassable |
             CollisionGroup.VaultImpassable);

        /// <summary>
        /// Ask for the pathfinder to gimme somethin
        /// </summary>
        /// <param name="pathfindingArgs"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Job<Queue<TileRef>> RequestPath(PathfindingArgs pathfindingArgs, CancellationToken cancellationToken)
        {
            var startNode = GetNode(pathfindingArgs.Start);
            var endNode = GetNode(pathfindingArgs.End);
            var job = new AStarPathfindingJob(0.003, startNode, endNode, pathfindingArgs, cancellationToken);
            _pathfindingQueue.EnqueueJob(job);

            return job;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            // Make sure graph is updated, then get pathfinders
            ProcessGraphUpdates();
            _pathfindingQueue.Process();
        }

        private void ProcessGraphUpdates()
        {
            var totalUpdates = 0;

            foreach (var update in _collidableUpdateQueue)
            {
                if (!EntityManager.EntityExists(update.Owner)) continue;

                if (update.CanCollide)
                {
                    HandleEntityAdd(update.Owner);
                }
                else
                {
                    HandleEntityRemove(update.Owner);
                }

                totalUpdates++;
            }

            _collidableUpdateQueue.Clear();

            foreach (var update in _accessReaderUpdateQueue)
            {
                if (update.Enabled)
                {
                    HandleEntityAdd(update.Sender);
                }
                else
                {
                    HandleEntityRemove(update.Sender);
                }

                totalUpdates++;
            }

            _accessReaderUpdateQueue.Clear();

            foreach (var tile in _tileUpdateQueue)
            {
                HandleTileUpdate(tile);
                totalUpdates++;
            }

            _tileUpdateQueue.Clear();
            var moveUpdateCount = Math.Max(50 - totalUpdates, 0);

            // Other updates are high priority so for this we'll just defer it if there's a spike (explosion, etc.)
            // If the move updates grow too large then we'll just do it
            if (_moveUpdateQueue.Count > 100)
            {
                moveUpdateCount = _moveUpdateQueue.Count - 100;
            }

            moveUpdateCount = Math.Min(moveUpdateCount, _moveUpdateQueue.Count);

            for (var i = 0; i < moveUpdateCount; i++)
            {
                HandleEntityMove(_moveUpdateQueue.Dequeue());
            }

            DebugTools.Assert(_moveUpdateQueue.Count < 1000);
        }

        public PathfindingChunk GetChunk(TileRef tile)
        {
            var chunkX = (int) (Math.Floor((float) tile.X / PathfindingChunk.ChunkSize) * PathfindingChunk.ChunkSize);
            var chunkY = (int) (Math.Floor((float) tile.Y / PathfindingChunk.ChunkSize) * PathfindingChunk.ChunkSize);
            var vector2i = new Vector2i(chunkX, chunkY);

            if (_graph.TryGetValue(tile.GridIndex, out var chunks))
            {
                if (!chunks.ContainsKey(vector2i))
                {
                    CreateChunk(tile.GridIndex, vector2i);
                }

                return chunks[vector2i];
            }

            var newChunk = CreateChunk(tile.GridIndex, vector2i);
            return newChunk;
        }

        private PathfindingChunk CreateChunk(GridId gridId, Vector2i indices)
        {
            var newChunk = new PathfindingChunk(gridId, indices);
            if (!_graph.ContainsKey(gridId))
            {
                _graph.Add(gridId, new Dictionary<Vector2i, PathfindingChunk>());
            }

            _graph[gridId].Add(indices, newChunk);
            newChunk.Initialize(_mapManager.GetGrid(gridId));

            return newChunk;
        }

        /// <summary>
        /// Get the entity's tile position, then get the corresponding node
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public PathfindingNode GetNode(EntityUid entity)
        {
            var tile = _mapManager.GetGrid(EntityManager.GetComponent<TransformComponent>(entity).GridID).GetTileRef(EntityManager.GetComponent<TransformComponent>(entity).Coordinates);
            return GetNode(tile);
        }

        /// <summary>
        /// Return the corresponding PathfindingNode for this tile
        /// </summary>
        /// <param name="tile"></param>
        /// <returns></returns>
        public PathfindingNode GetNode(TileRef tile)
        {
            var chunk = GetChunk(tile);
            var node = chunk.GetNode(tile);

            return node;
        }

        public override void Initialize()
        {
            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
            SubscribeLocalEvent<CollisionChangeMessage>(QueueCollisionChangeMessage);
            SubscribeLocalEvent<MoveEvent>(QueueMoveEvent);
            SubscribeLocalEvent<AccessReaderChangeMessage>(QueueAccessChangeMessage);

            // Handle all the base grid changes
            // Anything that affects traversal (i.e. collision layer) is handled separately.
            _mapManager.OnGridRemoved += HandleGridRemoval;
            _mapManager.GridChanged += QueueGridChange;
            _mapManager.TileChanged += QueueTileChange;
        }

        public override void Shutdown()
        {
            base.Shutdown();

            _mapManager.OnGridRemoved -= HandleGridRemoval;
            _mapManager.GridChanged -= QueueGridChange;
            _mapManager.TileChanged -= QueueTileChange;
        }

        private void HandleTileUpdate(TileRef tile)
        {
            if (!_mapManager.GridExists(tile.GridIndex)) return;

            var node = GetNode(tile);
            node.UpdateTile(tile);
        }

        private void HandleGridRemoval(MapId mapId, GridId gridId)
        {
            if (_graph.ContainsKey(gridId))
            {
                _graph.Remove(gridId);
            }
        }

        private void QueueGridChange(object? sender, GridChangedEventArgs eventArgs)
        {
            foreach (var (position, _) in eventArgs.Modified)
            {
                _tileUpdateQueue.Enqueue(eventArgs.Grid.GetTileRef(position));
            }
        }

        private void QueueTileChange(object? sender, TileChangedEventArgs eventArgs)
        {
            _tileUpdateQueue.Enqueue(eventArgs.NewTile);
        }

        private void QueueAccessChangeMessage(AccessReaderChangeMessage message)
        {
            _accessReaderUpdateQueue.Enqueue(message);
        }

        /// <summary>
        /// Tries to add the entity to the relevant pathfinding node
        /// </summary>
        /// The node will filter it to the correct category (if possible)
        /// <param name="entity"></param>
        private void HandleEntityAdd(EntityUid entity)
        {
            if (Deleted(entity) ||
                _lastKnownPositions.ContainsKey(entity) ||
                !EntityManager.TryGetComponent(entity, out IPhysBody? physics) ||
                !PathfindingNode.IsRelevant(entity, physics))
            {
                return;
            }

            var grid = _mapManager.GetGrid(EntityManager.GetComponent<TransformComponent>(entity).GridID);
            var tileRef = grid.GetTileRef(EntityManager.GetComponent<TransformComponent>(entity).Coordinates);

            var chunk = GetChunk(tileRef);
            var node = chunk.GetNode(tileRef);
            node.AddEntity(entity, physics);
            _lastKnownPositions.Add(entity, node);
        }

        private void HandleEntityRemove(EntityUid entity)
        {
            if (!_lastKnownPositions.TryGetValue(entity, out var node))
            {
                return;
            }

            node.RemoveEntity(entity);
            _lastKnownPositions.Remove(entity);
        }

        private void QueueMoveEvent(ref MoveEvent moveEvent)
        {
            _moveUpdateQueue.Enqueue(moveEvent);
        }

        /// <summary>
        /// When an entity moves around we'll remove it from its old node and add it to its new node (if applicable)
        /// </summary>
        /// <param name="moveEvent"></param>
        private void HandleEntityMove(MoveEvent moveEvent)
        {
            // If we've moved to space or the likes then remove us.
            if ((!EntityManager.EntityExists(moveEvent.Sender) ? EntityLifeStage.Deleted : EntityManager.GetComponent<MetaDataComponent>(moveEvent.Sender).EntityLifeStage) >= EntityLifeStage.Deleted ||
                !EntityManager.TryGetComponent(moveEvent.Sender, out IPhysBody? physics) ||
                !PathfindingNode.IsRelevant(moveEvent.Sender, physics) ||
                moveEvent.NewPosition.GetGridId(EntityManager) == GridId.Invalid)
            {
                HandleEntityRemove(moveEvent.Sender);
                return;
            }

            // Memory leak protection until grid parenting confirmed fix / you REALLY need the performance
            var gridBounds = _mapManager.GetGrid(EntityManager.GetComponent<TransformComponent>(moveEvent.Sender).GridID).WorldBounds;

            if (!gridBounds.Contains(EntityManager.GetComponent<TransformComponent>(moveEvent.Sender).WorldPosition))
            {
                HandleEntityRemove(moveEvent.Sender);
                return;
            }

            // If we move from space to a grid we may need to start tracking it.
            if (!_lastKnownPositions.TryGetValue(moveEvent.Sender, out var oldNode))
            {
                HandleEntityAdd(moveEvent.Sender);
                return;
            }

            var newGridId = moveEvent.NewPosition.GetGridId(_entityManager);
            if (newGridId == GridId.Invalid)
            {
                HandleEntityRemove(moveEvent.Sender);
                return;
            }

            // The pathfinding graph is tile-based so first we'll check if they're on a different tile and if we need to update.
            // If you get entities bigger than 1 tile wide you'll need some other system so god help you.
            var newTile = _mapManager.GetGrid(newGridId).GetTileRef(moveEvent.NewPosition);

            if (oldNode == null || oldNode.TileRef == newTile)
            {
                return;
            }

            var newNode = GetNode(newTile);
            _lastKnownPositions[moveEvent.Sender] = newNode;

            oldNode.RemoveEntity(moveEvent.Sender);
            newNode.AddEntity(moveEvent.Sender, physics);
        }

        private void QueueCollisionChangeMessage(CollisionChangeMessage collisionMessage)
        {
            _collidableUpdateQueue.Enqueue(collisionMessage);
        }

        // TODO: Need to rethink the pathfinder utils (traversable etc.). Maybe just chuck them all in PathfindingSystem
        // Otherwise you get the steerer using this and the pathfinders using a different traversable.
        // Also look at increasing tile cost the more physics entities are on it
        public bool CanTraverse(EntityUid entity, EntityCoordinates coordinates)
        {
            var gridId = coordinates.GetGridId(EntityManager);
            var tile = _mapManager.GetGrid(gridId).GetTileRef(coordinates);
            var node = GetNode(tile);
            return CanTraverse(entity, node);
        }

        public bool CanTraverse(EntityUid entity, PathfindingNode node)
        {
            if (EntityManager.TryGetComponent(entity, out IPhysBody? physics) &&
                (physics.CollisionMask & node.BlockedCollisionMask) != 0)
            {
                return false;
            }

            var access = _accessReader.FindAccessTags(entity);
            foreach (var reader in node.AccessReaders)
            {
                if (!_accessReader.IsAllowed(reader, access))
                {
                    return false;
                }
            }

            return true;
        }

        public void Reset(RoundRestartCleanupEvent ev)
        {
            _graph.Clear();
            _collidableUpdateQueue.Clear();
            _moveUpdateQueue.Clear();
            _accessReaderUpdateQueue.Clear();
            _tileUpdateQueue.Clear();
            _lastKnownPositions.Clear();
        }
    }
}
