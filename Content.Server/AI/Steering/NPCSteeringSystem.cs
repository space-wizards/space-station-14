using System.Linq;
using System.Threading;
using Content.Server.AI.Components;
using Content.Server.AI.Pathfinding;
using Content.Server.AI.Pathfinding.Pathfinders;
using Content.Server.CPUJob.JobQueues;
using Content.Shared.Access.Systems;
using Content.Shared.CCVar;
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

        // This will likely get moved onto an abstract pathfinding node that specifies the max distance allowed from the coordinate.
        private const float TileTolerance = 0.1f;

        private bool _enabled;

        public override void Initialize()
        {
            base.Initialize();
            _configManager.OnValueChanged(CCVars.NPCEnabled, SetNPCEnabled, true);

            SubscribeLocalEvent<NPCSteeringComponent, ComponentShutdown>(OnSteeringShutdown);
        }

        private void OnSteeringShutdown(EntityUid uid, NPCSteeringComponent component, ComponentShutdown args)
        {
            component.PathfindToken?.Cancel();
        }

        private void SetNPCEnabled(bool obj)
        {
            if (!obj)
            {
                foreach (var (_, mover) in EntityQuery<NPCSteeringComponent, InputMoverComponent>())
                {
                    mover.CurTickSprintMovement = Vector2.Zero;
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
        public NPCSteeringComponent Register(EntityUid entity, EntityCoordinates coordinates)
        {
            if (TryComp<NPCSteeringComponent>(entity, out var comp))
            {
                comp.PathfindToken?.Cancel();
                comp.PathfindToken = null;
                comp.CurrentPath.Clear();
            }
            else
            {
                comp = AddComp<NPCSteeringComponent>(entity);
            }

            comp.Coordinates = coordinates;
            return comp;
        }

        /// <summary>
        /// Stops the steering behavior for the AI and cleans up.
        /// </summary>
        public void Unregister(EntityUid uid, NPCSteeringComponent? component = null)
        {
            if (!Resolve(uid, ref component, false))
                return;

            if (EntityManager.TryGetComponent(component.Owner, out InputMoverComponent? controller))
            {
                controller.CurTickSprintMovement = Vector2.Zero;
            }

            component.PathfindToken?.Cancel();
            component.PathfindToken = null;
            component.Pathfind = null;
            RemComp<NPCSteeringComponent>(uid);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            if (!_enabled)
                return;

            // Not every mob has the modifier component so do it as a separate query.
            var bodyQuery = GetEntityQuery<PhysicsComponent>();
            var modifierQuery = GetEntityQuery<MovementSpeedModifierComponent>();

            foreach (var (steering, _, mover, xform) in EntityQuery<NPCSteeringComponent, ActiveNPCComponent, InputMoverComponent, TransformComponent>())
            {
                Steer(steering, mover, xform, modifierQuery, bodyQuery, frameTime);
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
        private void Steer(
            NPCSteeringComponent steering,
            InputMoverComponent mover,
            TransformComponent xform,
            EntityQuery<MovementSpeedModifierComponent> modifierQuery,
            EntityQuery<PhysicsComponent> bodyQuery,
            float frameTime)
        {
            var ourCoordinates = xform.Coordinates;
            var destinationCoordinates = steering.Coordinates;

            // We've arrived, nothing else matters.
            if (xform.Coordinates.TryDistance(EntityManager, destinationCoordinates, out var distance) &&
                distance <= steering.Range)
            {
                SetDirection(mover, Vector2.Zero);
                steering.Status = SteeringStatus.InRange;
                return;
            }

            // Can't move at all, just noop input.
            if (!mover.CanMove)
            {
                SetDirection(mover, Vector2.Zero);
                steering.Status = SteeringStatus.Moving;
                return;
            }

            // If we were pathfinding then try to update our path.
            if (steering.Pathfind != null)
            {
                switch (steering.Pathfind.Status)
                {
                    case JobStatus.Waiting:
                    case JobStatus.Running:
                    case JobStatus.Pending:
                    case JobStatus.Paused:
                        break;
                    case JobStatus.Finished:
                        steering.CurrentPath.Clear();

                        if (steering.Pathfind.Result != null)
                        {
                            PrunePath(ourCoordinates, steering.Pathfind.Result);

                            foreach (var node in steering.Pathfind.Result)
                            {
                                steering.CurrentPath.Enqueue(node);
                            }
                        }

                        steering.Pathfind = null;
                        steering.PathfindToken = null;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // Grab the target position, either the path or our end goal.
            // TODO: Some situations we may not want to move at our target without a path.
            var targetCoordinates = GetTargetCoordinates(steering);
            var arrivalDistance = TileTolerance;

            if (targetCoordinates.Equals(steering.Coordinates))
            {
                // What's our tolerance for arrival.
                // If it's a pathfinding node it might be different to the destination.
                arrivalDistance = steering.Range;
            }

            // Check if mapids match.
            var targetMap = targetCoordinates.ToMap(EntityManager);
            var ourMap = ourCoordinates.ToMap(EntityManager);

            if (targetMap.MapId != ourMap.MapId)
            {
                SetDirection(mover, Vector2.Zero);
                steering.Status = SteeringStatus.NoPath;
                return;
            }

            var direction = targetMap.Position - ourMap.Position;

            // Are we in range
            if (direction.Length <= arrivalDistance)
            {
                // It was just a node, not the target, so grab the next destination (either the target or next node).
                if (steering.CurrentPath.Count > 0)
                {
                    steering.CurrentPath.Dequeue();

                    // Alright just adjust slightly and grab the next node so we don't stop moving for a tick.
                    // TODO: If it's the last node just grab the target instead.
                    targetCoordinates = GetTargetCoordinates(steering);
                    targetMap = targetCoordinates.ToMap(EntityManager);

                    // Can't make it again.
                    if (ourMap.MapId != targetMap.MapId)
                    {
                        SetDirection(mover, Vector2.Zero);
                        steering.Status = SteeringStatus.NoPath;
                        return;
                    }

                    // Gonna resume now business as usual
                    direction = targetMap.Position - ourMap.Position;
                }
                else
                {
                    // This probably shouldn't happen as we check above but eh.
                    SetDirection(mover, Vector2.Zero);
                    steering.Status = SteeringStatus.InRange;
                    return;
                }
            }

            // Do we have no more nodes to follow OR has the target moved sufficiently? If so then re-path.
            var needsPath = steering.CurrentPath.Count == 0;

            // TODO: Probably need partial planning support i.e. patch from the last node to where the target moved to.

            if (!needsPath)
            {
                var lastNode = steering.CurrentPath.Last();
                // I know this is bad and doesn't account for tile size
                // However with the path I'm going to change it to return pathfinding nodes which include coordinates instead.
                var lastCoordinate = new EntityCoordinates(lastNode.GridUid, (Vector2) lastNode.GridIndices + 0.5f);

                if (lastCoordinate.TryDistance(EntityManager, steering.Coordinates, out var lastDistance) &&
                    lastDistance > steering.RepathRange)
                {
                    needsPath = true;
                }
            }

            // Request the new path.
            if (needsPath && bodyQuery.TryGetComponent(steering.Owner, out var body))
            {
                RequestPath(steering, xform, body);
            }

            modifierQuery.TryGetComponent(steering.Owner, out var modifier);
            var moveSpeed = GetSprintSpeed(modifier);

            var input = direction.Normalized;

            // If we're going to overshoot then... don't.
            // TODO: For tile / movement we don't need to get bang on, just need to make sure we don't overshoot the far end.
            var tickMovement = moveSpeed * frameTime;

            if (tickMovement.Equals(0f))
            {
                SetDirection(mover, Vector2.Zero);
                steering.Status = SteeringStatus.NoPath;
                return;
            }

            // We may overshoot slightly but still be in the arrival distance which is okay.
            var maxDistance = direction.Length + arrivalDistance;

            if (tickMovement > maxDistance)
            {
                input *= maxDistance / tickMovement;
            }

            // TODO: This isn't going to work for space.
            if (_mapManager.TryGetGrid(xform.GridUid, out var grid))
            {
                input = (-grid.WorldRotation).RotateVec(input);
            }

            SetDirection(mover, input);

            // todo: Need a console command to make an NPC steer to a specific spot.

            // TODO: Actual steering behaviours and collision avoidance.
            // TODO: Need to handle path invalidation if nodes change.
        }

        /// <summary>
        /// We may be pathfinding and moving at the same time in which case early nodes may be out of date.
        /// </summary>
        private void PrunePath(EntityCoordinates coordinates, Queue<TileRef> nodes)
        {
            // Right now the pathfinder gives EVERY TILE back but ideally it won't someday, it'll just give straightline ones.
            // For now, we just prune up until the closest node + 1 extra.
            var closest = ((Vector2) nodes.Peek().GridIndices + 0.5f - coordinates.Position).Length;
            // TODO: Need to handle multi-grid and stuff.

            while (nodes.TryPeek(out var node))
            {
                // TODO: Tile size
                var nodePosition = (Vector2) node.GridIndices + 0.5f;
                var length = (coordinates.Position - nodePosition).Length;

                if (length < closest)
                {
                    closest = length;
                    nodes.Dequeue();
                    continue;
                }

                nodes.Dequeue();
                break;
            }
        }

        /// <summary>
        /// Get the coordinates we should be heading towards.
        /// </summary>
        private EntityCoordinates GetTargetCoordinates(NPCSteeringComponent steering)
        {
            // Depending on what's going on we may return the target or a pathfind node.

            // If it's the last node then just head to the target.
            if (steering.CurrentPath.Count > 1 && steering.CurrentPath.TryPeek(out var nextTarget))
            {
                return new EntityCoordinates(nextTarget.GridUid, (Vector2) nextTarget.GridIndices + 0.5f);
            }

            return steering.Coordinates;
        }

        /// <summary>
        /// Get a new job from the pathfindingsystem
        /// </summary>
        private void RequestPath(NPCSteeringComponent steering, TransformComponent xform, PhysicsComponent? body)
        {
            // If we already have a pathfinding request then don't grab another.
            if (steering.Pathfind != null)
                return;

            if (!_mapManager.TryGetGrid(xform.GridUid, out var grid))
                return;

            steering.PathfindToken = new CancellationTokenSource();
            var startTile = grid.GetTileRef(xform.Coordinates);
            var endTile = grid.GetTileRef(steering.Coordinates);
            var collisionMask = 0;

            if (body != null)
            {
                collisionMask = body.CollisionMask;
            }

            var access = _accessReader.FindAccessTags(steering.Owner);

            steering.Pathfind = _pathfindingSystem.RequestPath(new PathfindingArgs(
                steering.Owner,
                access,
                collisionMask,
                startTile,
                endTile,
                steering.Range
            ), steering.PathfindToken.Token);
        }

        private float GetSprintSpeed(MovementSpeedModifierComponent? modifier)
        {
            return modifier?.CurrentSprintSpeed ?? MovementSpeedModifierComponent.DefaultBaseSprintSpeed;
        }

        private float GetWalkSpeed(MovementSpeedModifierComponent? modifier)
        {
            return modifier?.CurrentWalkSpeed ?? MovementSpeedModifierComponent.DefaultBaseWalkSpeed;
        }
    }
}
