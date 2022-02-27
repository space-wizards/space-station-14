using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.AI.Components;
using Content.Server.AI.Pathfinding;
using Content.Server.AI.Pathfinding.Pathfinders;
using Content.Server.CPUJob.JobQueues;
using Content.Shared.Access.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.AI.Steering
{
    public sealed class AiSteeringSystem : EntitySystem
    {
        // http://www.red3d.com/cwr/papers/1999/gdc99steer.html for a steering overview
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly PathfindingSystem _pathfindingSystem = default!;
        [Dependency] private readonly AccessReaderSystem _accessReader = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

        /// <summary>
        /// Whether we try to avoid non-blocking physics objects
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CollisionAvoidanceEnabled { get; set; } = true;

        /// <summary>
        /// How close we need to get to the center of each tile
        /// </summary>
        private const float TileTolerance = 0.8f;

        /// <summary>
        ///     How long to wait between checks (if necessary).
        /// </summary>
        private const float InRangeUnobstructedCooldown = 0.25f;

        private Dictionary<EntityUid, IAiSteeringRequest> RunningAgents => _agentLists[_listIndex];

        // We'll cycle the running list every tick as all we're doing is getting a vector2 for the
        // agent's steering. Should help a lot given this is the most expensive operator by far.
        // The AI will keep moving, it's just it'll keep moving in its existing direction.
        // If we change to 20/30 TPS you might want to change this but for now it's fine
        private readonly List<Dictionary<EntityUid, IAiSteeringRequest>> _agentLists = new(AgentListCount);
        private const int AgentListCount = 2;
        private int _listIndex;

        // Cache nextGrid
        private readonly Dictionary<EntityUid, EntityCoordinates> _nextGrid = new();

        /// <summary>
        /// Current live paths for AI
        /// </summary>
        private readonly Dictionary<EntityUid, Queue<TileRef>> _paths = new();

        /// <summary>
        /// Pathfinding request jobs we're waiting on
        /// </summary>
        private readonly Dictionary<EntityUid, (CancellationTokenSource CancelToken, CPUJob.JobQueues.Job<Queue<TileRef>> Job)> _pathfindingRequests =
            new();

        /// <summary>
        /// Keep track of how long we've been in 1 position and re-path if it's been too long
        /// </summary>
        private readonly Dictionary<EntityUid, int> _stuckCounter = new();

        /// <summary>
        /// Get a fixed position for the target entity; if they move then re-path
        /// </summary>
        private readonly Dictionary<EntityUid, EntityCoordinates> _entityTargetPosition = new();

        // Anti-Stuck
        // Given the collision avoidance can lead to twitching need to store a reference position and check if we've been near this too long
        private readonly Dictionary<EntityUid, EntityCoordinates> _stuckPositions = new();

        public override void Initialize()
        {
            base.Initialize();

            for (var i = 0; i < AgentListCount; i++)
            {
                _agentLists.Add(new Dictionary<EntityUid, IAiSteeringRequest>());
            }
        }

        /// <summary>
        /// Adds the AI to the steering system to move towards a specific target
        /// </summary>
        /// We'll add it to the movement list that has the least number of agents
        /// <param name="entity"></param>
        /// <param name="steeringRequest"></param>
        public void Register(EntityUid entity, IAiSteeringRequest steeringRequest)
        {
            var lowestListCount = 1000;
            var lowestListIndex = 0;

            for (var i = 0; i < _agentLists.Count; i++)
            {
                var agentList = _agentLists[i];
                // Register shouldn't be called twice; if it is then someone dun fucked up
                DebugTools.Assert(!agentList.ContainsKey(entity));

                if (agentList.Count < lowestListCount)
                {
                    lowestListCount = agentList.Count;
                    lowestListIndex = i;
                }
            }

            _agentLists[lowestListIndex].Add(entity, steeringRequest);
        }

        /// <summary>
        /// Stops the steering behavior for the AI and cleans up
        /// </summary>
        /// <param name="entity"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void Unregister(EntityUid entity)
        {
            if (EntityManager.TryGetComponent(entity, out AiControllerComponent? controller))
            {
                controller.VelocityDir = Vector2.Zero;
            }

            if (_pathfindingRequests.TryGetValue(entity, out var request))
            {
                switch (request.Job.Status)
                {
                    case JobStatus.Pending:
                    case JobStatus.Finished:
                        break;
                    case JobStatus.Running:
                    case JobStatus.Paused:
                    case JobStatus.Waiting:
                        request.CancelToken.Cancel();
                        break;
                }

                switch (request.Job.Exception)
                {
                    case null:
                        break;
                    default:
                        ExceptionDispatchInfo.Capture(request.Job.Exception).Throw();
                        throw request.Job.Exception;
                }
                _pathfindingRequests.Remove(entity);
            }

            if (_paths.ContainsKey(entity))
            {
                _paths.Remove(entity);
            }

            if (_nextGrid.ContainsKey(entity))
            {
                _nextGrid.Remove(entity);
            }

            if (_stuckCounter.ContainsKey(entity))
            {
                _stuckCounter.Remove(entity);
            }

            if (_entityTargetPosition.ContainsKey(entity))
            {
                _entityTargetPosition.Remove(entity);
            }

            foreach (var agentList in _agentLists)
            {
                if (agentList.ContainsKey(entity))
                {
                    agentList.Remove(entity);
                    return;
                }
            }
        }

        /// <summary>
        /// Is the entity currently registered for steering?
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool IsRegistered(EntityUid entity)
        {
            foreach (var agentList in _agentLists)
            {
                if (agentList.ContainsKey(entity))
                {
                    return true;
                }
            }

            return false;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var (agent, steering) in RunningAgents)
            {
                // Yeah look it's not true frametime but good enough.
                var result = Steer(agent, steering, frameTime * RunningAgents.Count);
                steering.Status = result;

                switch (result)
                {
                    case SteeringStatus.Pending:
                        break;
                    case SteeringStatus.NoPath:
                        Unregister(agent);
                        break;
                    case SteeringStatus.Arrived:
                        Unregister(agent);
                        break;
                    case SteeringStatus.Moving:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            _listIndex = (_listIndex + 1) % _agentLists.Count;
        }

        /// <summary>
        /// Go through each steerer and combine their vectors
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="steeringRequest"></param>
        /// <param name="frameTime"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private SteeringStatus Steer(EntityUid entity, IAiSteeringRequest steeringRequest, float frameTime)
        {
            // Main optimisation to be done below is the redundant calls and adding more variables
            if (Deleted(entity) ||
                !EntityManager.TryGetComponent(entity, out AiControllerComponent? controller) ||
                !EntitySystem.Get<ActionBlockerSystem>().CanMove(entity) ||
                !EntityManager.GetComponent<TransformComponent>(entity).GridID.IsValid())
            {
                return SteeringStatus.NoPath;
            }

            var entitySteering = steeringRequest as EntityTargetSteeringRequest;

            if (entitySteering != null && (!EntityManager.EntityExists(entitySteering.Target) ? EntityLifeStage.Deleted : EntityManager.GetComponent<MetaDataComponent>(entitySteering.Target).EntityLifeStage) >= EntityLifeStage.Deleted)
            {
                controller.VelocityDir = Vector2.Zero;
                return SteeringStatus.NoPath;
            }

            if (_mapManager.IsGridPaused(EntityManager.GetComponent<TransformComponent>(entity).GridID))
            {
                controller.VelocityDir = Vector2.Zero;
                return SteeringStatus.Pending;
            }

            // Validation
            // Check if we can even arrive -> Currently only samegrid movement supported
            if (EntityManager.GetComponent<TransformComponent>(entity).GridID != steeringRequest.TargetGrid.GetGridId(EntityManager))
            {
                controller.VelocityDir = Vector2.Zero;
                return SteeringStatus.NoPath;
            }

            // Check if we have arrived
            var targetDistance = (EntityManager.GetComponent<TransformComponent>(entity).MapPosition.Position - steeringRequest.TargetMap.Position).Length;
            steeringRequest.TimeUntilInteractionCheck -= frameTime;

            if (targetDistance <= steeringRequest.ArrivalDistance && steeringRequest.TimeUntilInteractionCheck <= 0.0f)
            {
                if (!steeringRequest.RequiresInRangeUnobstructed ||
                    _interactionSystem.InRangeUnobstructed(entity, steeringRequest.TargetMap, steeringRequest.ArrivalDistance, popup: true))
                {
                    // TODO: Need cruder LOS checks for ranged weaps
                    controller.VelocityDir = Vector2.Zero;
                    return SteeringStatus.Arrived;
                }

                steeringRequest.TimeUntilInteractionCheck = InRangeUnobstructedCooldown;
                // Welp, we'll keep on moving.
            }

            // If we're really close don't swiggity swoogity back and forth and just wait for the interaction check maybe?
            if (steeringRequest.TimeUntilInteractionCheck > 0.0f && targetDistance <= 0.1f)
            {
                controller.VelocityDir = Vector2.Zero;
                return SteeringStatus.Moving;
            }

            // Handle pathfinding job
            // If we still have an existing path then keep following that until the new path arrives
            if (_pathfindingRequests.TryGetValue(entity, out var pathRequest) && pathRequest.Job.Status == JobStatus.Finished)
            {
                switch (pathRequest.Job.Exception)
                {
                    case null:
                        break;
                    // Currently nothing should be cancelling these except external factors
                    case TaskCanceledException _:
                        controller.VelocityDir = Vector2.Zero;
                        return SteeringStatus.NoPath;
                    default:
                        throw pathRequest.Job.Exception;
                }
                // No actual path
                var path = _pathfindingRequests[entity].Job.Result;
                if (path == null || path.Count == 0)
                {
                    controller.VelocityDir = Vector2.Zero;
                    return SteeringStatus.NoPath;
                }

                // If we're closer to next tile then we don't want to walk backwards to our tile's center
                UpdatePath(entity, path);

                // If we're targeting entity get a fixed tile; if they move from it then re-path (at least til we get a better solution)
                if (entitySteering != null)
                {
                    _entityTargetPosition[entity] = entitySteering.TargetGrid;
                }

                // Move next tick
                return SteeringStatus.Pending;
            }

            // Check if we even have a path to follow
            // If the route's empty we could be close and may not need a re-path so we won't check if it is
            if (!_paths.ContainsKey(entity) && !_pathfindingRequests.ContainsKey(entity) && targetDistance > 1.5f)
            {
                controller.VelocityDir = Vector2.Zero;
                RequestPath(entity, steeringRequest);
                return SteeringStatus.Pending;
            }

            var ignoredCollision = new List<EntityUid>();
            // Check if the target entity has moved - If so then re-path
            // TODO: Patch the path from the target's position back towards us, stopping if it ever intersects the current path
            // Probably need a separate "PatchPath" job
            if (entitySteering != null)
            {
                // Check if target's moved too far
                if (_entityTargetPosition.TryGetValue(entity, out var targetGrid) &&
                    (entitySteering.TargetGrid.Position - targetGrid.Position).Length >= entitySteering.TargetMaxMove)
                {
                    // We'll just repath and keep following the existing one until we get a new one
                    RequestPath(entity, steeringRequest);
                }

                ignoredCollision.Add(entitySteering.Target);
            }

            HandleStuck(entity);

            // TODO: Probably need a dedicated queuing solver (doorway congestion FML)
            // Get the target grid (either next tile or target itself) and pass it in to the steering behaviors
            // If there's nowhere to go then just stop and wait
            var nextGrid = NextGrid(entity, steeringRequest);
            if (!nextGrid.HasValue)
            {
                controller.VelocityDir = Vector2.Zero;
                return SteeringStatus.NoPath;
            }

            // Validate that we can even get to the next grid (could probably just check if we can use nextTile if we're not near the target grid)
            if (!_pathfindingSystem.CanTraverse(entity, nextGrid.Value))
            {
                controller.VelocityDir = Vector2.Zero;
                return SteeringStatus.NoPath;
            }

            // Now we can /finally/ move
            var movementVector = Vector2.Zero;

            // Originally I tried using interface steerers but ehhh each one kind of needs to do its own thing
            // Plus there's not much point putting these in a separate class
            // Each one just adds onto the final vector
            movementVector += Seek(entity, nextGrid.Value);
            if (CollisionAvoidanceEnabled)
            {
                movementVector += CollisionAvoidance(entity, movementVector, ignoredCollision);
            }
            // Group behaviors would also go here e.g. separation, cohesion, alignment

            // Move towards it
            DebugTools.Assert(movementVector != new Vector2(float.NaN, float.NaN));
            controller.VelocityDir = movementVector.Normalized;
            return SteeringStatus.Moving;
        }

        /// <summary>
        /// Get a new job from the pathfindingsystem
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="steeringRequest"></param>
        private void RequestPath(EntityUid entity, IAiSteeringRequest steeringRequest)
        {
            if (_pathfindingRequests.ContainsKey(entity))
            {
                return;
            }

            var cancelToken = new CancellationTokenSource();
            var gridManager = _mapManager.GetGrid(EntityManager.GetComponent<TransformComponent>(entity).GridID);
            var startTile = gridManager.GetTileRef(EntityManager.GetComponent<TransformComponent>(entity).Coordinates);
            var endTile = gridManager.GetTileRef(steeringRequest.TargetGrid);
            var collisionMask = 0;
            if (EntityManager.TryGetComponent(entity, out IPhysBody? physics))
            {
                collisionMask = physics.CollisionMask;
            }

            var access = _accessReader.FindAccessTags(entity);

            var job = _pathfindingSystem.RequestPath(new PathfindingArgs(
                entity,
                access,
                collisionMask,
                startTile,
                endTile,
                steeringRequest.PathfindingProximity
            ), cancelToken.Token);
            _pathfindingRequests.Add(entity, (cancelToken, job));
        }

        /// <summary>
        /// Given the pathfinding is timesliced we need to trim the first few(?) tiles so we don't walk backwards
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="path"></param>
        private void UpdatePath(EntityUid entity, Queue<TileRef> path)
        {
            _pathfindingRequests.Remove(entity);

            var entityTile = _mapManager.GetGrid(EntityManager.GetComponent<TransformComponent>(entity).GridID).GetTileRef(EntityManager.GetComponent<TransformComponent>(entity).Coordinates);
            var tile = path.Dequeue();
            var closestDistance = PathfindingHelpers.OctileDistance(entityTile, tile);

            for (var i = 0; i < path.Count; i++)
            {
                tile = path.Peek();
                var distance = PathfindingHelpers.OctileDistance(entityTile, tile);
                if (distance < closestDistance)
                {
                    path.Dequeue();
                }
                else
                {
                    break;
                }
            }

            _paths[entity] = path;
        }

        /// <summary>
        /// Get the next tile as EntityCoordinates
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="steeringRequest"></param>
        /// <returns></returns>
        private EntityCoordinates? NextGrid(EntityUid entity, IAiSteeringRequest steeringRequest)
        {
            // Remove the cached grid
            if (!_paths.ContainsKey(entity) && _nextGrid.ContainsKey(entity))
            {
                _nextGrid.Remove(entity);
            }

            // If no tiles left just move towards the target (if we're close)
            if (!_paths.ContainsKey(entity) || _paths[entity].Count == 0)
            {
                if ((steeringRequest.TargetGrid.Position - EntityManager.GetComponent<TransformComponent>(entity).Coordinates.Position).Length <= 2.0f)
                {
                    return steeringRequest.TargetGrid;
                }

                // Too far so we need a re-path
                return null;
            }

            if (!_nextGrid.TryGetValue(entity, out var nextGrid) ||
                (nextGrid.Position - EntityManager.GetComponent<TransformComponent>(entity).Coordinates.Position).Length <= TileTolerance)
            {
                UpdateGridCache(entity);
                nextGrid = _nextGrid[entity];
            }

            DebugTools.Assert(nextGrid != default);
            return nextGrid;
        }

        /// <summary>
        /// Rather than converting TileRef to EntityCoordinates over and over we'll just cache it
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="dequeue"></param>
        private void UpdateGridCache(EntityUid entity, bool dequeue = true)
        {
            if (_paths[entity].Count == 0) return;
            var nextTile = dequeue ? _paths[entity].Dequeue() : _paths[entity].Peek();
            var nextGrid = _mapManager.GetGrid(EntityManager.GetComponent<TransformComponent>(entity).GridID).GridTileToLocal(nextTile.GridIndices);
            _nextGrid[entity] = nextGrid;
        }

        /// <summary>
        /// Check if we've been near our last EntityCoordinates too long and try to fix it
        /// </summary>
        /// <param name="entity"></param>
        private void HandleStuck(EntityUid entity)
        {
            if (!_stuckPositions.TryGetValue(entity, out var stuckPosition))
            {
                _stuckPositions[entity] = EntityManager.GetComponent<TransformComponent>(entity).Coordinates;
                _stuckCounter[entity] = 0;
                return;
            }

            if ((EntityManager.GetComponent<TransformComponent>(entity).Coordinates.Position - stuckPosition.Position).Length <= 1.0f)
            {
                _stuckCounter.TryGetValue(entity, out var stuckCount);
                _stuckCounter[entity] = stuckCount + 1;
            }
            else
            {
                // No longer stuck
                _stuckPositions[entity] = EntityManager.GetComponent<TransformComponent>(entity).Coordinates;
                _stuckCounter[entity] = 0;
                return;
            }

            // Should probably be time-based
            if (_stuckCounter[entity] < 30)
            {
                return;
            }

            // Okay now we're stuck
            _paths.Remove(entity);
            _stuckCounter[entity] = 0;
        }

        #region Steering
        /// <summary>
        /// Move straight to target position
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="grid"></param>
        /// <returns></returns>
        private Vector2 Seek(EntityUid entity, EntityCoordinates grid)
        {
            // is-even much
            var entityPos = EntityManager.GetComponent<TransformComponent>(entity).Coordinates;
            return entityPos == grid
                ? Vector2.Zero
                : (grid.Position - entityPos.Position).Normalized;
        }

        /// <summary>
        /// Like Seek but slows down when within distance
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="grid"></param>
        /// <param name="slowingDistance"></param>
        /// <returns></returns>
        private Vector2 Arrival(EntityUid entity, EntityCoordinates grid, float slowingDistance = 1.0f)
        {
            var entityPos = EntityManager.GetComponent<TransformComponent>(entity).Coordinates;
            DebugTools.Assert(slowingDistance > 0.0f);
            if (entityPos == grid)
            {
                return Vector2.Zero;
            }
            var targetDiff = grid.Position - entityPos.Position;
            var rampedSpeed = targetDiff.Length / slowingDistance;
            return targetDiff.Normalized * MathF.Min(1.0f, rampedSpeed);
        }

        /// <summary>
        /// Like Seek but predicts target's future position
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private Vector2 Pursuit(EntityUid entity, EntityUid target)
        {
            var entityPos = EntityManager.GetComponent<TransformComponent>(entity).Coordinates;
            var targetPos = EntityManager.GetComponent<TransformComponent>(target).Coordinates;
            if (entityPos == targetPos)
            {
                return Vector2.Zero;
            }

            if (EntityManager.TryGetComponent(target, out IPhysBody? physics))
            {
                var targetDistance = (targetPos.Position - entityPos.Position);
                targetPos = targetPos.Offset(physics.LinearVelocity * targetDistance);
            }

            return (targetPos.Position - entityPos.Position).Normalized;
        }

        /// <summary>
        /// Checks for non-anchored physics objects that can block us
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="direction">entity's travel direction</param>
        /// <param name="ignoredTargets"></param>
        /// <returns></returns>
        private Vector2 CollisionAvoidance(EntityUid entity, Vector2 direction, ICollection<EntityUid> ignoredTargets)
        {
            if (direction == Vector2.Zero || !EntityManager.TryGetComponent(entity, out IPhysBody? physics))
            {
                return Vector2.Zero;
            }

            // We'll check tile-by-tile
            // Rewriting this frequently so not many comments as they'll go stale
            // I realise this is bad so please rewrite it ;-;
            var entityCollisionMask = physics.CollisionMask;
            var avoidanceVector = Vector2.Zero;
            var checkTiles = new HashSet<TileRef>();
            var avoidTiles = new HashSet<TileRef>();
            var entityGridCoords = EntityManager.GetComponent<TransformComponent>(entity).Coordinates;
            var grid = _mapManager.GetGrid(EntityManager.GetComponent<TransformComponent>(entity).GridID);
            var currentTile = grid.GetTileRef(entityGridCoords);
            var halfwayTile = grid.GetTileRef(entityGridCoords.Offset(direction / 2));
            var nextTile = grid.GetTileRef(entityGridCoords.Offset(direction));

            checkTiles.Add(currentTile);
            checkTiles.Add(halfwayTile);
            checkTiles.Add(nextTile);

            // Handling corners with collision avoidance is a real bitch
            // TBH collision avoidance in general that doesn't run like arse is a real bitch
            foreach (var tile in checkTiles)
            {
                var node = _pathfindingSystem.GetNode(tile);
                // Assume the immovables have already been checked
                foreach (var (physicsEntity, layer) in node.PhysicsLayers)
                {
                    // Ignore myself / my target if applicable / if my mask doesn't collide
                    if (physicsEntity == entity || ignoredTargets.Contains(physicsEntity) || (entityCollisionMask & layer) == 0) continue;
                    // God there's so many ways to do this
                    // err for now we'll just assume the first entity is the center and just add a vector for it

                    //Pathfinding updates are deferred so this may not be done yet.
                    if (Deleted(physicsEntity)) continue;

                    // if we're moving in the same direction then ignore
                    // So if 2 entities are moving towards each other and both detect a collision they'll both move in the same direction
                    // i.e. towards the right
                    if (EntityManager.TryGetComponent(physicsEntity, out IPhysBody? otherPhysics) &&
                        Vector2.Dot(otherPhysics.LinearVelocity, direction) > 0)
                    {
                        continue;
                    }

                    var centerGrid = EntityManager.GetComponent<TransformComponent>(physicsEntity).Coordinates;
                    // Check how close we are to center of tile and get the inverse; if we're closer this is stronger
                    var additionalVector = (centerGrid.Position - entityGridCoords.Position);
                    var distance = additionalVector.Length;
                    // If we're too far no point, if we're close then cap it at the normalized vector
                    distance = MathHelper.Clamp(2.5f - distance, 0.0f, 1.0f);
                    additionalVector = new Angle(90 * distance).RotateVec(additionalVector);
                    avoidanceVector += additionalVector;
                    // if we do need to avoid that means we'll have to lookahead for the next tile
                    avoidTiles.Add(tile);
                    break;
                }
            }

            // Dis ugly
            if (_paths.TryGetValue(entity, out var path))
            {
                if (path.Count > 0)
                {
                    var checkTile = path.Peek();
                    for (var i = 0; i < Math.Min(path.Count, avoidTiles.Count); i++)
                    {
                        if (avoidTiles.Contains(checkTile))
                        {
                            checkTile = path.Dequeue();
                        }
                    }

                    UpdateGridCache(entity, false);
                }
            }

            return avoidanceVector == Vector2.Zero ? avoidanceVector : avoidanceVector.Normalized;
        }
        #endregion
    }

    public enum SteeringStatus
    {
        Pending,
        NoPath,
        Arrived,
        Moving,
    }
}
