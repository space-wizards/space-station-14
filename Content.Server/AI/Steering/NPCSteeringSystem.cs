using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.AI.Components;
using Content.Server.AI.Pathfinding;
using Content.Server.AI.Pathfinding.Pathfinders;
using Content.Server.CPUJob.JobQueues;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
using Content.Shared.Doors.Components;
using Content.Shared.Interaction;
using Content.Shared.Movement.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.AI.Steering
{
    public sealed class NPCSteeringSystem : EntitySystem
    {
        // http://www.red3d.com/cwr/papers/1999/gdc99steer.html for a steering overview
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly PathfindingSystem _pathfindingSystem = default!;
        [Dependency] private readonly AccessReaderSystem _accessReader = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

        private bool _enabled;

        public override void Initialize()
        {
            base.Initialize();
            _configManager.OnValueChanged(CCVars.NPCEnabled, SetNPCEnabled, true);
        }

        private void SetNPCEnabled(bool obj)
        {
            if (!obj)
            {
                foreach (var comp in EntityQuery<NPCSteeringComponent>())
                {
                    comp.LastInput = Vector2.Zero;
                }
            }

            _enabled = obj;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _configManager.UnsubValueChanged(CCVars.NPCEnabled, SetNPCEnabled);
        }

        /// <summary>
        /// Adds the AI to the steering system to move towards a specific target
        /// </summary>
        public void Register(EntityUid entity, EntityCoordinates coordinates)
        {
            NPCSteeringComponent? comp;

            if (TryComp(entity, out comp))
            {
                comp.PathfindToken?.Cancel();
                comp.PathfindToken = null;
                comp.CurrentPath.Clear();
                comp.LastInput = Vector2.Zero;
            }
            else
            {
                comp = AddComp<NPCSteeringComponent>(entity);
            }

            comp.Coordinates = coordinates;
        }

        /// <summary>
        /// Stops the steering behavior for the AI and cleans up
        /// </summary>
        /// <param name="entity"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void Unregister(NPCSteeringComponent component)
        {
            if (EntityManager.TryGetComponent(component.Owner, out InputMoverComponent? controller))
            {
                controller.CurTickSprintMovement = Vector2.Zero;
            }

            component.PathfindToken?.Cancel();
            component.PathfindToken = null;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!_enabled)
                return;

            foreach (var (steering, _, mover) in EntityQuery<NPCSteeringComponent, ActiveNPCComponent, InputMoverComponent, TransformComponent>())
            {
                Steer(steering, mover, xform, frameTime);
            }
        }

        private void SetDirection(InputMoverComponent component, Vector2 value)
        {
            component.CurTickSprintMovement = value;
            component.LastInputTick = _timing.CurTick;
            component.LastInputSubTick = ushort.MaxValue;
        }

        /// <summary>
        /// Go through each steerer and combine their vectors
        /// </summary>
        private void Steer(NPCSteeringComponent steering, InputMoverComponent mover, TransformComponent xform, float frameTime)
        {
            if (!mover.CanMove ||
                xform.GridUid == null)
            {
                SetDirection(mover, Vector2.Zero);
                return;
            }

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

            var xform = EntityManager.GetComponent<TransformComponent>(entity);
            if (xform.GridUid == null)
                return;

            var cancelToken = new CancellationTokenSource();
            var gridManager = _mapManager.GetGrid(xform.GridUid.Value);
            var startTile = gridManager.GetTileRef(xform.Coordinates);
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

            var xform = EntityManager.GetComponent<TransformComponent>(entity);
            if (xform.GridUid == null)
                return;
            var entityTile = _mapManager.GetGrid(xform.GridUid.Value).GetTileRef(xform.Coordinates);
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

            var xform = EntityManager.GetComponent<TransformComponent>(entity);

            // If no tiles left just move towards the target (if we're close)
            if (!_paths.ContainsKey(entity) || _paths[entity].Count == 0)
            {
                if ((steeringRequest.TargetGrid.Position - xform.Coordinates.Position).Length <= 2.0f)
                {
                    return steeringRequest.TargetGrid;
                }

                // Too far so we need a re-path
                return null;
            }

            if (!_nextGrid.TryGetValue(entity, out var nextGrid) ||
                (nextGrid.Position - xform.Coordinates.Position).Length <= TileTolerance)
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

            var xform = EntityManager.GetComponent<TransformComponent>(entity);
            if (xform.GridUid == null)
                return;

            var nextGrid = _mapManager.GetGrid(xform.GridUid.Value).GridTileToLocal(nextTile.GridIndices);
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

            var xform = EntityManager.GetComponent<TransformComponent>(entity);
            if (xform.GridUid == null)
                return default;

            var entityGridCoords = xform.Coordinates;
            var grid = _mapManager.GetGrid(xform.GridUid.Value);
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
                        (!otherPhysics.Hard ||
                        Vector2.Dot(otherPhysics.LinearVelocity, direction) > 0))
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
