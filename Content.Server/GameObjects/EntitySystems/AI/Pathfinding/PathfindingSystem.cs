using System;
using System.Collections.Generic;
using System.Threading;
using Content.Server.GameObjects.Components.Access;
using Content.Server.GameObjects.EntitySystems.AI.Pathfinding.Pathfinders;
using Content.Server.GameObjects.EntitySystems.JobQueues;
using Content.Server.GameObjects.EntitySystems.JobQueues.Queues;
using Content.Server.GameObjects.EntitySystems.Pathfinding;
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
    // TODO: IMO use rectangular symmetry reduction on the nodes with collision at all., or
    alternatively store all rooms and have an alternative graph for humanoid mobs (same collision mask, needs access etc). You could also just path from room to room as needed.
    // TODO: Longer term -> Handle collision layer changes?
    */
    /// <summary>
    /// This system handles pathfinding graph updates as well as dispatches to the pathfinder
    /// (90% of what it's doing is graph updates so not much point splitting the 2 roles)
    /// </summary>
    public class PathfindingSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IEntityManager _entitymanager;
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        public IReadOnlyDictionary<GridId, Dictionary<MapIndices, PathfindingChunk>> Graph => _graph;
        private readonly Dictionary<GridId, Dictionary<MapIndices, PathfindingChunk>> _graph = new Dictionary<GridId, Dictionary<MapIndices, PathfindingChunk>>();
        
        private readonly PathfindingJobQueue _pathfindingQueue = new PathfindingJobQueue();
        
        // Queued pathfinding graph updates
        private readonly Queue<CollisionChangeEvent> _collidableUpdateQueue = new Queue<CollisionChangeEvent>();
        private readonly Queue<MoveEvent> _moveUpdateQueue = new Queue<MoveEvent>();
        private readonly Queue<AccessReaderChangeMessage> _accessReaderUpdateQueue = new Queue<AccessReaderChangeMessage>();
        private readonly Queue<TileRef> _tileUpdateQueue = new Queue<TileRef>();

        // Need to store previously known entity positions for collidables for when they move
        private readonly Dictionary<IEntity, TileRef> _lastKnownPositions = new Dictionary<IEntity, TileRef>();

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
                var entity = _entitymanager.GetEntity(update.Owner);
                if (update.CanCollide)
                {
                    HandleCollidableAdd(entity);
                }
                else
                {
                    HandleAccessRemove(entity);
                }

                totalUpdates++;
            }
            
            _collidableUpdateQueue.Clear();

            foreach (var update in _accessReaderUpdateQueue)
            {
                var entity = _entitymanager.GetEntity(update.Uid);
                if (update.Enabled)
                {
                    HandleAccessAdd(entity);
                }
                else
                {
                    HandleAccessRemove(entity);
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
                HandleCollidableMove(_moveUpdateQueue.Dequeue());
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
            newChunk.Initialize();
            if (_graph.TryGetValue(gridId, out var chunks))
            {
                for (var x = -1; x < 2; x++)
                {
                    for (var y = -1; y < 2; y++)
                    {
                        if (x == 0 && y == 0) continue;

                        var neighborIndices = new MapIndices(
                            indices.X + x * PathfindingChunk.ChunkSize,
                            indices.Y + y * PathfindingChunk.ChunkSize);

                        if (chunks.TryGetValue(neighborIndices, out var neighborChunk))
                        {
                            neighborChunk.AddNeighbor(newChunk);
                        }
                    }
                }
            }
            else
            {
                _graph.Add(gridId, new Dictionary<MapIndices, PathfindingChunk>());
            }

            _graph[gridId].Add(indices, newChunk);

            return newChunk;
        }

        public PathfindingNode GetNode(TileRef tile)
        {
            var chunk = GetChunk(tile);
            var node = chunk.GetNode(tile);

            return node;
        }

        public override void Initialize()
        {
            SubscribeLocalEvent<CollisionChangeEvent>(QueueCollisionEnabledEvent);
            SubscribeLocalEvent<MoveEvent>(QueueCollidableMove);
            SubscribeLocalEvent<AccessReaderChangeMessage>(QueueAccessChangeEvent);

            // Handle all the base grid changes
            // Anything that affects traversal (i.e. collision layer) is handled separately.
            _mapManager.OnGridRemoved += HandleGridRemoval;
            _mapManager.GridChanged += QueueGridChange;
            _mapManager.TileChanged += QueueTileChange;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            UnsubscribeLocalEvent<CollisionChangeEvent>();
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

        private void QueueAccessChangeEvent(AccessReaderChangeMessage message)
        {
            _accessReaderUpdateQueue.Enqueue(message);
        }

        private void HandleAccessAdd(IEntity entity)
        {
            if (entity.Deleted || !entity.HasComponent<AccessReader>())
            {
                return;
            }
            
            var grid = _mapManager.GetGrid(entity.Transform.GridID);
            var tileRef = grid.GetTileRef(entity.Transform.GridPosition);

            var chunk = GetChunk(tileRef);
            var node = chunk.GetNode(tileRef);
            node.AddEntity(entity);
        }

        private void HandleAccessRemove(IEntity entity)
        {
            if (entity.Deleted || !entity.HasComponent<AccessReader>())
            {
                return;
            }
            
            var grid = _mapManager.GetGrid(entity.Transform.GridID);
            var tileRef = grid.GetTileRef(entity.Transform.GridPosition);

            var chunk = GetChunk(tileRef);
            var node = chunk.GetNode(tileRef);
            node.RemoveEntity(entity);
        }

        #region collidable
        /// <summary>
        /// If an entity's collision gets turned on then we need to update its current position
        /// </summary>
        /// <param name="entity"></param>
        private void HandleCollidableAdd(IEntity entity)
        {
            if (entity.Prototype == null ||
                entity.Deleted ||
                _lastKnownPositions.ContainsKey(entity) || 
                !entity.TryGetComponent(out CollidableComponent collidableComponent) || 
                !collidableComponent.CanCollide || 
                (TrackedCollisionLayers & collidableComponent.CollisionLayer) == 0)
            {
                return;
            }

            var grid = _mapManager.GetGrid(entity.Transform.GridID);
            var tileRef = grid.GetTileRef(entity.Transform.GridPosition);
            var chunk = GetChunk(tileRef);
            var node = chunk.GetNode(tileRef);

            node.AddEntity(entity);
            _lastKnownPositions.Add(entity, tileRef);
        }

        /// <summary>
        /// If an entity's collision is removed then stop tracking it from the graph
        /// </summary>
        /// <param name="entity"></param>
        private void HandleCollidableRemove(IEntity entity)
        {
            if (entity.Prototype == null ||
                entity.Deleted ||
                !_lastKnownPositions.ContainsKey(entity) || 
                !entity.TryGetComponent(out CollidableComponent collidableComponent) || 
                !collidableComponent.CanCollide || 
                (TrackedCollisionLayers & collidableComponent.CollisionLayer) == 0)
            {
                return;
            }

            var grid = _mapManager.GetGrid(entity.Transform.GridID);
            var tileRef = grid.GetTileRef(entity.Transform.GridPosition);
            var chunk = GetChunk(tileRef);
            var node = chunk.GetNode(tileRef);

            node.RemoveEntity(entity);
            _lastKnownPositions.Remove(entity);
        }

        private void QueueCollidableMove(MoveEvent moveEvent)
        {
            _moveUpdateQueue.Enqueue(moveEvent);
        }

        private void HandleCollidableMove(MoveEvent moveEvent)
        {
            if (!_lastKnownPositions.ContainsKey(moveEvent.Sender))
            {
                return;
            }

            // The pathfinding graph is tile-based so first we'll check if they're on a different tile and if we need to update.
            // If you get entities bigger than 1 tile wide you'll need some other system so god help you.
            if (moveEvent.Sender.Deleted)
            {
                HandleCollidableRemove(moveEvent.Sender);
                return;
            }

            _lastKnownPositions.TryGetValue(moveEvent.Sender, out var oldTile);
            var newTile = _mapManager.GetGrid(moveEvent.NewPosition.GridID).GetTileRef(moveEvent.NewPosition);

            if (oldTile == newTile)
            {
                return;
            }

            _lastKnownPositions[moveEvent.Sender] = newTile;

            if (!moveEvent.Sender.HasComponent<CollidableComponent>())
            {
                HandleCollidableRemove(moveEvent.Sender);
                return;
            }

            var gridIds = new HashSet<GridId>(2) {oldTile.GridIndex, newTile.GridIndex};

            foreach (var gridId in gridIds)
            {
                if (oldTile.GridIndex == gridId)
                {
                    var oldChunk = GetChunk(oldTile);
                    var oldNode = oldChunk.GetNode(oldTile);
                    oldNode.RemoveEntity(moveEvent.Sender);
                }

                if (newTile.GridIndex == gridId)
                {
                    var newChunk = GetChunk(newTile);
                    var newNode = newChunk.GetNode(newTile);
                    newNode.AddEntity(moveEvent.Sender);
                }
            }
        }

        private void QueueCollisionEnabledEvent(CollisionChangeEvent collisionEvent)
        {
            _collidableUpdateQueue.Enqueue(collisionEvent);
        }
        #endregion

        // TODO: Need to rethink the pathfinder utils (traversable etc.). Maybe just chuck them all in PathfindingSystem
        // Otherwise you get the steerer using this and the pathfinders using a different traversable.
        // Also look at increasing tile cost the more physics entities are on it
        public bool CanTraverse(IEntity entity, GridCoordinates grid)
        {
            var tile = _mapManager.GetGrid(grid.GridID).GetTileRef(grid);
            var node = GetNode(tile);
            return CanTraverse(entity, node);
        }

        public bool CanTraverse(IEntity entity, PathfindingNode node)
        {
            if (entity.TryGetComponent(out CollidableComponent collidableComponent) &&
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
