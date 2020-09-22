using System;
using System.Collections.Generic;
using System.Threading;
using Content.Server.GameObjects.Components.Access;
using Content.Server.GameObjects.EntitySystems.AI.Pathfinding.Pathfinders;
using Content.Server.GameObjects.EntitySystems.JobQueues;
using Content.Server.GameObjects.EntitySystems.JobQueues.Queues;
using Content.Shared.Physics;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.EntitySystems.AI.Pathfinding
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

        public IReadOnlyDictionary<GridId, Dictionary<MapIndices, PathfindingChunk>> Graph => _graph;
        private readonly Dictionary<GridId, Dictionary<MapIndices, PathfindingChunk>> _graph = new Dictionary<GridId, Dictionary<MapIndices, PathfindingChunk>>();

        private readonly PathfindingJobQueue _pathfindingQueue = new PathfindingJobQueue();

        // Queued pathfinding graph updates
        private readonly Queue<CollisionChangeMessage> _collidableUpdateQueue = new Queue<CollisionChangeMessage>();
        private readonly Queue<MoveEvent> _moveUpdateQueue = new Queue<MoveEvent>();
        private readonly Queue<AccessReaderChangeMessage> _accessReaderUpdateQueue = new Queue<AccessReaderChangeMessage>();
        private readonly Queue<TileRef> _tileUpdateQueue = new Queue<TileRef>();

        // Need to store previously known entity positions for collidables for when they move
        private readonly Dictionary<IEntity, PathfindingNode> _lastKnownPositions = new Dictionary<IEntity, PathfindingNode>();

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
                var entity = EntityManager.GetEntity(update.Owner);
                if (update.CanCollide)
                {
                    HandleEntityAdd(entity);
                }
                else
                {
                    HandleEntityRemove(entity);
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
            var mapIndices = new MapIndices(chunkX, chunkY);

            if (_graph.TryGetValue(tile.GridIndex, out var chunks))
            {
                if (!chunks.ContainsKey(mapIndices))
                {
                    CreateChunk(tile.GridIndex, mapIndices);
                }

                return chunks[mapIndices];
            }

            var newChunk = CreateChunk(tile.GridIndex, mapIndices);
            return newChunk;
        }

        private PathfindingChunk CreateChunk(GridId gridId, MapIndices indices)
        {
            var newChunk = new PathfindingChunk(gridId, indices);
            if (!_graph.ContainsKey(gridId))
            {
                _graph.Add(gridId, new Dictionary<MapIndices, PathfindingChunk>());
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
        public PathfindingNode GetNode(IEntity entity)
        {
            var tile = _mapManager.GetGrid(entity.Transform.GridID).GetTileRef(entity.Transform.Coordinates);
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
            UnsubscribeLocalEvent<CollisionChangeMessage>();
            UnsubscribeLocalEvent<MoveEvent>();
            UnsubscribeLocalEvent<AccessReaderChangeMessage>();

            _mapManager.OnGridRemoved -= HandleGridRemoval;
            _mapManager.GridChanged -= QueueGridChange;
            _mapManager.TileChanged -= QueueTileChange;
        }

        private void HandleTileUpdate(TileRef tile)
        {
            var node = GetNode(tile);
            node.UpdateTile(tile);
        }

        public void ResettingCleanup()
        {
            _graph.Clear();
            _collidableUpdateQueue.Clear();
            _moveUpdateQueue.Clear();
            _accessReaderUpdateQueue.Clear();
            _tileUpdateQueue.Clear();
            _lastKnownPositions.Clear();
        }

        private void HandleGridRemoval(GridId gridId)
        {
            if (_graph.ContainsKey(gridId))
            {
                _graph.Remove(gridId);
            }
        }

        private void QueueGridChange(object sender, GridChangedEventArgs eventArgs)
        {
            foreach (var (position, _) in eventArgs.Modified)
            {
                _tileUpdateQueue.Enqueue(eventArgs.Grid.GetTileRef(position));
            }
        }

        private void QueueTileChange(object sender, TileChangedEventArgs eventArgs)
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
        private void HandleEntityAdd(IEntity entity)
        {
            if (entity.Deleted ||
                _lastKnownPositions.ContainsKey(entity) ||
                !entity.TryGetComponent(out ICollidableComponent collidableComponent) ||
                !PathfindingNode.IsRelevant(entity, collidableComponent))
            {
                return;
            }

            var grid = _mapManager.GetGrid(entity.Transform.GridID);
            var tileRef = grid.GetTileRef(entity.Transform.Coordinates);

            var chunk = GetChunk(tileRef);
            var node = chunk.GetNode(tileRef);
            node.AddEntity(entity, collidableComponent);
            _lastKnownPositions.Add(entity, node);
        }

        private void HandleEntityRemove(IEntity entity)
        {
            if (!_lastKnownPositions.TryGetValue(entity, out var node))
            {
                return;
            }

            node.RemoveEntity(entity);
            _lastKnownPositions.Remove(entity);
        }

        private void QueueMoveEvent(MoveEvent moveEvent)
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
            if (moveEvent.Sender.Deleted ||
                !moveEvent.Sender.TryGetComponent(out ICollidableComponent collidableComponent) ||
                !PathfindingNode.IsRelevant(moveEvent.Sender, collidableComponent))
            {
                HandleEntityRemove(moveEvent.Sender);
                return;
            }

            // Memory leak protection until grid parenting confirmed fix / you REALLY need the performance
            var gridBounds = _mapManager.GetGrid(moveEvent.Sender.Transform.GridID).WorldBounds;

            if (!gridBounds.Contains(moveEvent.Sender.Transform.WorldPosition))
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

            // The pathfinding graph is tile-based so first we'll check if they're on a different tile and if we need to update.
            // If you get entities bigger than 1 tile wide you'll need some other system so god help you.
            var newTile = _mapManager.GetGrid(moveEvent.NewPosition.GetGridId(_entityManager)).GetTileRef(moveEvent.NewPosition);

            if (oldNode == null || oldNode.TileRef == newTile)
            {
                return;
            }

            var newNode = GetNode(newTile);
            _lastKnownPositions[moveEvent.Sender] = newNode;

            oldNode.RemoveEntity(moveEvent.Sender);
            newNode.AddEntity(moveEvent.Sender, collidableComponent);
        }

        private void QueueCollisionChangeMessage(CollisionChangeMessage collisionMessage)
        {
            _collidableUpdateQueue.Enqueue(collisionMessage);
        }

        // TODO: Need to rethink the pathfinder utils (traversable etc.). Maybe just chuck them all in PathfindingSystem
        // Otherwise you get the steerer using this and the pathfinders using a different traversable.
        // Also look at increasing tile cost the more physics entities are on it
        public bool CanTraverse(IEntity entity, EntityCoordinates coordinates)
        {
            var gridId = coordinates.GetGridId(_entityManager);
            var tile = _mapManager.GetGrid(gridId).GetTileRef(coordinates);
            var node = GetNode(tile);
            return CanTraverse(entity, node);
        }

        public bool CanTraverse(IEntity entity, PathfindingNode node)
        {
            if (entity.TryGetComponent(out ICollidableComponent collidableComponent) &&
                (collidableComponent.CollisionMask & node.BlockedCollisionMask) != 0)
            {
                return false;
            }

            var access = AccessReader.FindAccessTags(entity);

            foreach (var reader in node.AccessReaders)
            {
                if (!reader.IsAllowed(access))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
