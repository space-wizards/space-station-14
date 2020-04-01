using System;
using System.Collections.Generic;
using System.Threading;
using Content.Server.GameObjects.Components.Doors;
using Content.Server.GameObjects.Components.Pathfinding;
using Content.Server.GameObjects.EntitySystems.AI.Pathfinding.GraphUpdates;
using Content.Server.GameObjects.EntitySystems.AI.Pathfinding.Pathfinders;
using Content.Server.GameObjects.EntitySystems.JobQueues;
using Content.Server.GameObjects.EntitySystems.JobQueues.Queues;
using Content.Server.GameObjects.EntitySystems.Pathfinding;
using Content.Shared.Pathfinding;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.EntitySystems.AI.Pathfinding
{
    /*
    // TODO: IMO use rectangular symmetry reduction on the nodes with collision at all.
    // TODO: Look at storing different a graph for common masks (e.g. 1 for humans)
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

        public Dictionary<GridId, Dictionary<MapIndices, PathfindingChunk>> Graph => _graph;
        private readonly Dictionary<GridId, Dictionary<MapIndices, PathfindingChunk>> _graph = new Dictionary<GridId, Dictionary<MapIndices, PathfindingChunk>>();
        // Every tick we queue up all the changes and do them at once
        private readonly Queue<IPathfindingGraphUpdate> _queuedGraphUpdates = new Queue<IPathfindingGraphUpdate>();
        private readonly PathfindingJobQueue _pathfindingQueue = new PathfindingJobQueue();
#if DEBUG
        private readonly Queue<AStarRouteDebug> _aStarRouteDebugs = new Queue<AStarRouteDebug>();
        private readonly Queue<JpsRouteDebug> _jpsRouteDebugs = new Queue<JpsRouteDebug>();
#endif

        // Need to store previously known entity positions for collidables for when they move
        private readonly Dictionary<IEntity, TileRef> _lastKnownPositions = new Dictionary<IEntity, TileRef>();

        // TODO: Token support for jobs
        /// <summary>
        /// Ask for the pathfinder to gimme somethin
        /// </summary>
        /// <param name="pathfindingArgs"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Job<Queue<TileRef>> RequestPath(PathfindingArgs pathfindingArgs)
        {
            var startNode = GetNode(pathfindingArgs.Start);
            var endNode = GetNode(pathfindingArgs.End);
            var job = new JpsPathfindingJob(0.003, startNode, endNode, pathfindingArgs);
            _pathfindingQueue.PendingQueue.Enqueue(job);

            return job;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
#if DEBUG
            var componentManager = IoCManager.Resolve<IComponentManager>();
            foreach (var component in componentManager.GetAllComponents(typeof(ServerPathfindingDebugDebugComponent)))
            {
                foreach (var route in _aStarRouteDebugs)
                {
                    SendAStarDebugMessage((ServerPathfindingDebugDebugComponent) component, route);
                }
            }

            _aStarRouteDebugs.Clear();

            foreach (var component in componentManager.GetAllComponents(typeof(ServerPathfindingDebugDebugComponent)))
            {
                foreach (var route in _jpsRouteDebugs)
                {
                    SendJpsDebugMessage((ServerPathfindingDebugDebugComponent) component, route);
                }
            }

            _jpsRouteDebugs.Clear();
#endif

            // Make sure graph is updated, then get pathfinders
            ProcessGraphUpdates();
            _pathfindingQueue.Process();
        }

        private void ProcessGraphUpdates()
        {
            for (var i = 0; i < Math.Min(100, _queuedGraphUpdates.Count); i++)
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
                    case GridChange change:
                        throw new NotImplementedException();
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

        private Dictionary<MapIndices, PathfindingChunk> GetChunks(GridId gridId)
        {
            if (_graph.ContainsKey(gridId))
            {
                return _graph[gridId];
            }

            var grid = _mapManager.GetGrid(gridId);

            foreach (var tile in grid.GetAllTiles())
            {
                GetChunk(tile);
            }

            _graph.TryGetValue(gridId, out var chunks);

            return chunks ?? new Dictionary<MapIndices, PathfindingChunk>();
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
            if (_graph.TryGetValue(tile.GridIndex, out var chunks))
            {
                var chunkX = (int) (Math.Floor((float) tile.X / PathfindingChunk.ChunkSize) * PathfindingChunk.ChunkSize);
                var chunkY = (int) (Math.Floor((float) tile.Y / PathfindingChunk.ChunkSize) * PathfindingChunk.ChunkSize);
                var mapIndices = new MapIndices(chunkX, chunkY);

                if (chunks.TryGetValue(mapIndices, out var chunk))
                {
                    return chunk.GetNode(tile);
                }
            }

            return null;
        }

        public override void Initialize()
        {
            IoCManager.InjectDependencies(this);

            // Handle all the base grid changes
            // Anything that affects traversal (i.e. collision layer) is handled separately.
            _mapManager.OnGridRemoved += (id => { _queuedGraphUpdates.Enqueue(new GridRemoval(id)); });
            _mapManager.GridChanged += QueueGridChange;
            _mapManager.TileChanged += QueueTileChange;

#if DEBUG
            AStarPathfindingJob.DebugRoute += QueueAStarRouteDebug;
            JpsPathfindingJob.DebugRoute += QueueJpsRouteDebug;
#endif
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _mapManager.OnGridRemoved -= (id => { _queuedGraphUpdates.Enqueue(new GridRemoval(id)); });
            _mapManager.GridChanged -= QueueGridChange;
            _mapManager.TileChanged -= QueueTileChange;

#if DEBUG
            AStarPathfindingJob.DebugRoute -= QueueAStarRouteDebug;
            JpsPathfindingJob.DebugRoute -= QueueJpsRouteDebug;
#endif
        }

        public override void SubscribeEvents()
        {
            base.SubscribeEvents();
            SubscribeEvent<CollisionEnabledEvent>(QueueCollisionEnabledEvent);
        }

#if DEBUG
        private void QueueJpsRouteDebug(JpsRouteDebug routeDebug)
        {
            if (routeDebug?.Route != null)
            {
                _jpsRouteDebugs.Enqueue(routeDebug);
            }
        }

        private void QueueAStarRouteDebug(AStarRouteDebug routeDebug)
        {
            if (routeDebug?.Route != null)
            {
                _aStarRouteDebugs.Enqueue(routeDebug);
            }
        }

        /// <summary>
        /// Mainly here because it's currently easier to send as Vectors.
        /// </summary>
        /// <param name="debugComponent"></param>
        /// <param name="routeDebug"></param>
        private void SendAStarDebugMessage(ServerPathfindingDebugDebugComponent debugComponent, AStarRouteDebug routeDebug)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();
            var route = new List<Vector2>();
            foreach (var tile in routeDebug.Route)
            {
                var tileGrid = mapManager.GetGrid(tile.GridIndex).GridTileToLocal(tile.GridIndices);
                route.Add(mapManager.GetGrid(tile.GridIndex).LocalToWorld(tileGrid).Position);
            }

            var cameFrom = new Dictionary<Vector2, Vector2>();
            foreach (var (from, to) in routeDebug.CameFrom)
            {
                var tileOneGrid = mapManager.GetGrid(from.GridIndex).GridTileToLocal(from.GridIndices);
                var tileOneWorld = mapManager.GetGrid(from.GridIndex).LocalToWorld(tileOneGrid).Position;
                var tileTwoGrid = mapManager.GetGrid(to.GridIndex).GridTileToLocal(to.GridIndices);
                var tileTwoWorld = mapManager.GetGrid(to.GridIndex).LocalToWorld(tileTwoGrid).Position;
                cameFrom.Add(tileOneWorld, tileTwoWorld);
            }

            var gScores = new Dictionary<Vector2, float>();
            foreach (var (tile, score) in routeDebug.GScores)
            {
                var tileGrid = mapManager.GetGrid(tile.GridIndex).GridTileToLocal(tile.GridIndices);
                gScores.Add(mapManager.GetGrid(tile.GridIndex).LocalToWorld(tileGrid).Position, score);
            }

            var closedTiles = new List<Vector2>();
            foreach (var tile in routeDebug.ClosedTiles)
            {
                var tileGrid = mapManager.GetGrid(tile.GridIndex).GridTileToLocal(tile.GridIndices);
                closedTiles.Add(mapManager.GetGrid(tile.GridIndex).LocalToWorld(tileGrid).Position);
            }

            var msg = new AStarRouteMessage(
                routeDebug.EntityUid,
                route,
                cameFrom,
                gScores,
                closedTiles,
                routeDebug.TimeTaken
                );

            debugComponent.Owner.SendNetworkMessage(debugComponent, msg);
        }

        private void SendJpsDebugMessage(ServerPathfindingDebugDebugComponent debugComponent, JpsRouteDebug routeDebug)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();
            var route = new List<Vector2>();
            foreach (var tile in routeDebug.Route)
            {
                var tileGrid = mapManager.GetGrid(tile.GridIndex).GridTileToLocal(tile.GridIndices);
                route.Add(mapManager.GetGrid(tile.GridIndex).LocalToWorld(tileGrid).Position);
            }

            var jumpNodes = new List<Vector2>();
            foreach (var tile in routeDebug.JumpNodes)
            {
                var tileGrid = mapManager.GetGrid(tile.GridIndex).GridTileToLocal(tile.GridIndices);
                jumpNodes.Add(mapManager.GetGrid(tile.GridIndex).LocalToWorld(tileGrid).Position);
            }

            var msg = new JpsRouteMessage(
                routeDebug.EntityUid,
                route,
                jumpNodes,
                routeDebug.TimeTaken
                );

            debugComponent.Owner.SendNetworkMessage(debugComponent, msg);
        }
#endif

        private void QueueGridChange(object sender, GridChangedEventArgs eventArgs)
        {
            throw new NotImplementedException();
            _queuedGraphUpdates.Enqueue(new GridChange()); // TODO
        }

        private void QueueTileChange(object sender, TileChangedEventArgs eventArgs)
        {
            _queuedGraphUpdates.Enqueue(new TileUpdate(eventArgs.NewTile));
        }

        #region collidable
        /// <summary>
        /// If an entity's collision gets turned on then we need to start tracking it as it moves to update the graph
        /// </summary>
        /// <param name="entity"></param>
        private void HandleCollidableAdd(IEntity entity)
        {
            // It's a grid / gone / a door
            if (entity.Prototype == null ||
                entity.Deleted ||
                entity.HasComponent<ServerDoorComponent>() ||
                entity.HasComponent<AirlockComponent>())
            {
                return;
            }

            entity.Transform.OnMove += (sender, args) =>
            {
                QueueCollidableMove(args, entity);
            };
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
            if (_lastKnownPositions.ContainsKey(entity))
            {
                _lastKnownPositions.Remove(entity);
                // TODO: That spot will remain un-traversable 4EVA if it doesn't contain?
            }

            if (entity.Prototype == null ||
                entity.Deleted ||
                entity.HasComponent<ServerDoorComponent>() ||
                entity.HasComponent<AirlockComponent>())
            {
                return;
            }

            entity.Transform.OnMove -= (sender, args) =>
            {
                QueueCollidableMove(args, entity);
            };
            var grid = _mapManager.GetGrid(entity.Transform.GridID);
            var tileRef = grid.GetTileRef(entity.Transform.GridPosition);

            var collisionLayer = entity.GetComponent<CollidableComponent>().CollisionLayer;

            var chunk = GetChunk(tileRef);
            var node = chunk.GetNode(tileRef);
            node.RemoveCollisionLayer(collisionLayer);
        }

        private void QueueCollidableMove(MoveEventArgs eventArgs, IEntity entity)
        {
            // We'll check if it even needs queueing up first as the graph is stored per tile.
            if (!_lastKnownPositions.TryGetValue(entity, out var oldTile))
            {
                // This shouldn't happen
                Logger.WarningS("pathfinding", $"Handled collidable move for entity {entity.Uid} without a known position");
            }
            var newTile = _mapManager.GetGrid(eventArgs.NewPosition.GridID).GetTileRef(eventArgs.NewPosition);

            if (oldTile == newTile)
            {
                return;
            }

            _lastKnownPositions[entity] = newTile;
            var collisionLayer = entity.GetComponent<CollidableComponent>().CollisionLayer;

            _queuedGraphUpdates.Enqueue(new CollidableMove(collisionLayer, oldTile, newTile));
        }

        private void HandleCollidableMove(CollidableMove move)
        {
            var gridIds = new HashSet<GridId>(2) {move.OldTile.GridIndex, move.NewTile.GridIndex};

            foreach (var gridId in gridIds)
            {
                if (move.OldTile.GridIndex == gridId)
                {
                    var oldChunk = GetChunk(move.OldTile);
                    var oldNode = oldChunk.GetNode(move.OldTile);
                    oldNode.RemoveCollisionLayer(move.CollisionLayer);
                }

                if (move.NewTile.GridIndex == gridId)
                {
                    var newChunk = GetChunk(move.NewTile);
                    var newNode = newChunk.GetNode(move.NewTile);
                    newNode.RemoveCollisionLayer(move.CollisionLayer);
                }
            }
        }

        private void QueueCollisionEnabledEvent(object sender, CollisionEnabledEvent collisionEvent)
        {
            // TODO: Handle containers
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var entity = entityManager.GetEntity(collisionEvent.Owner);
            switch (collisionEvent.Value)
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
