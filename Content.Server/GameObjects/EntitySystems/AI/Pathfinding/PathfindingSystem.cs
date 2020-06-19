using System;
using System.Collections.Generic;
using System.Threading;
using Content.Server.GameObjects.Components.Doors;
using Content.Server.GameObjects.EntitySystems.AI.Pathfinding.GraphUpdates;
using Content.Server.GameObjects.EntitySystems.AI.Pathfinding.Pathfinders;
using Content.Server.GameObjects.EntitySystems.JobQueues;
using Content.Server.GameObjects.EntitySystems.JobQueues.Queues;
using Content.Server.GameObjects.EntitySystems.Pathfinding;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

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
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        public IReadOnlyDictionary<GridId, Dictionary<MapIndices, PathfindingChunk>> Graph => _graph;
        private readonly Dictionary<GridId, Dictionary<MapIndices, PathfindingChunk>> _graph = new Dictionary<GridId, Dictionary<MapIndices, PathfindingChunk>>();
        // Every tick we queue up all the changes and do them at once
        private readonly Queue<IPathfindingGraphUpdate> _queuedGraphUpdates = new Queue<IPathfindingGraphUpdate>();
        private readonly PathfindingJobQueue _pathfindingQueue = new PathfindingJobQueue();

        // Need to store previously known entity positions for collidables for when they move
        private readonly Dictionary<IEntity, TileRef> _lastKnownPositions = new Dictionary<IEntity, TileRef>();

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
            for (var i = 0; i < Math.Min(50, _queuedGraphUpdates.Count); i++)
            {
                var update = _queuedGraphUpdates.Dequeue();
                switch (update)
                {
                    case CollidableMove move:
                        HandleCollidableMove(move);
                        break;
                    case CollisionChange change:
                        if (change.Value)
                        {
                            HandleCollidableAdd(change.Owner);
                        }
                        else
                        {
                            HandleCollidableRemove(change.Owner);
                        }

                        break;
                    case GridRemoval removal:
                        HandleGridRemoval(removal);
                        break;
                    case TileUpdate tile:
                        HandleTileUpdate(tile);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private void HandleGridRemoval(GridRemoval removal)
        {
            if (!_graph.ContainsKey(removal.GridId))
            {
                throw new InvalidOperationException();
            }

            _graph.Remove(removal.GridId);
        }

        private void HandleTileUpdate(TileUpdate tile)
        {
            var chunk = GetChunk(tile.Tile);
            chunk.UpdateNode(tile.Tile);
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
            IoCManager.InjectDependencies(this);
            SubscribeLocalEvent<CollisionChangeEvent>(QueueCollisionEnabledEvent);
            SubscribeLocalEvent<MoveEvent>(QueueCollidableMove);

            // Handle all the base grid changes
            // Anything that affects traversal (i.e. collision layer) is handled separately.
            _mapManager.OnGridRemoved += QueueGridRemoval;
            _mapManager.GridChanged += QueueGridChange;
            _mapManager.TileChanged += QueueTileChange;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _mapManager.OnGridRemoved -= QueueGridRemoval;
            _mapManager.GridChanged -= QueueGridChange;
            _mapManager.TileChanged -= QueueTileChange;
        }

        private void QueueGridRemoval(GridId gridId)
        {
            _queuedGraphUpdates.Enqueue(new GridRemoval(gridId));
        }

        private void QueueGridChange(object sender, GridChangedEventArgs eventArgs)
        {
            foreach (var (position, _) in eventArgs.Modified)
            {
                _queuedGraphUpdates.Enqueue(new TileUpdate(eventArgs.Grid.GetTileRef(position)));
            }
        }

        private void QueueTileChange(object sender, TileChangedEventArgs eventArgs)
        {
            _queuedGraphUpdates.Enqueue(new TileUpdate(eventArgs.NewTile));
        }

        #region collidable
        /// <summary>
        /// If an entity's collision gets turned on then we need to update its current position
        /// </summary>
        /// <param name="entity"></param>
        private void HandleCollidableAdd(IEntity entity)
        {
            // It's a grid / gone / a door / we already have it (which probably shouldn't happen)
            if (entity.Prototype == null ||
                entity.Deleted ||
                entity.HasComponent<ServerDoorComponent>() ||
                entity.HasComponent<AirlockComponent>() ||
                _lastKnownPositions.ContainsKey(entity))
            {
                return;
            }

            var grid = _mapManager.GetGrid(entity.Transform.GridID);
            var tileRef = grid.GetTileRef(entity.Transform.GridPosition);

            var collisionLayer = entity.GetComponent<CollidableComponent>().CollisionLayer;

            var chunk = GetChunk(tileRef);
            var node = chunk.GetNode(tileRef);
            node.AddCollisionLayer(collisionLayer);

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
                entity.HasComponent<ServerDoorComponent>() ||
                entity.HasComponent<AirlockComponent>() ||
                !_lastKnownPositions.ContainsKey(entity))
            {
                return;
            }

            _lastKnownPositions.Remove(entity);

            var grid = _mapManager.GetGrid(entity.Transform.GridID);
            var tileRef = grid.GetTileRef(entity.Transform.GridPosition);

            if (!entity.TryGetComponent(out CollidableComponent collidableComponent))
            {
                return;
            }

            var collisionLayer = collidableComponent.CollisionLayer;

            var chunk = GetChunk(tileRef);
            var node = chunk.GetNode(tileRef);
            node.RemoveCollisionLayer(collisionLayer);
        }

        private void QueueCollidableMove(MoveEvent moveEvent)
        {
            _queuedGraphUpdates.Enqueue(new CollidableMove(moveEvent));
        }

        private void HandleCollidableMove(CollidableMove move)
        {
            if (!_lastKnownPositions.ContainsKey(move.MoveEvent.Sender))
            {
                return;
            }

            // The pathfinding graph is tile-based so first we'll check if they're on a different tile and if we need to update.
            // If you get entities bigger than 1 tile wide you'll need some other system so god help you.
            var moveEvent = move.MoveEvent;

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

            if (!moveEvent.Sender.TryGetComponent(out CollidableComponent collidableComponent))
            {
                HandleCollidableRemove(moveEvent.Sender);
                return;
            }

            var collisionLayer = collidableComponent.CollisionLayer;

            var gridIds = new HashSet<GridId>(2) {oldTile.GridIndex, newTile.GridIndex};

            foreach (var gridId in gridIds)
            {
                if (oldTile.GridIndex == gridId)
                {
                    var oldChunk = GetChunk(oldTile);
                    var oldNode = oldChunk.GetNode(oldTile);
                    oldNode.RemoveCollisionLayer(collisionLayer);
                }

                if (newTile.GridIndex == gridId)
                {
                    var newChunk = GetChunk(newTile);
                    var newNode = newChunk.GetNode(newTile);
                    newNode.RemoveCollisionLayer(collisionLayer);
                }
            }
        }

        private void QueueCollisionEnabledEvent(CollisionChangeEvent collisionEvent)
        {
            // TODO: Handle containers
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var entity = entityManager.GetEntity(collisionEvent.Owner);
            switch (collisionEvent.CanCollide)
            {
                case true:
                    _queuedGraphUpdates.Enqueue(new CollisionChange(entity, true));
                    break;
                case false:
                    _queuedGraphUpdates.Enqueue(new CollisionChange(entity, false));
                    break;
            }
        }
        #endregion
    }
}
