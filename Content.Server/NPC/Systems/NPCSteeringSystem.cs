using System.Linq;
using System.Threading;
using Content.Server.Administration.Managers;
using Content.Server.Doors.Systems;
using Content.Server.NPC.Components;
using Content.Server.NPC.Events;
using Content.Server.NPC.Pathfinding;
using Content.Shared.CCVar;
using Content.Shared.Interaction;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.NPC;
using Content.Shared.NPC.Events;
using Content.Shared.Weapons.Melee;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.NPC.Systems
{
    public sealed partial class NPCSteeringSystem : SharedNPCSteeringSystem
    {
        // http://www.red3d.com/cwr/papers/1999/gdc99steer.html for a steering overview
        [Dependency] private readonly IAdminManager _admin = default!;
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly DoorSystem _doors = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly PathfindingSystem _pathfindingSystem = default!;
        [Dependency] private readonly SharedInteractionSystem _interaction = default!;
        [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
        [Dependency] private readonly SharedMoverController _mover = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;

        // This will likely get moved onto an abstract pathfinding node that specifies the max distance allowed from the coordinate.
        private const float TileTolerance = 0.40f;

        private bool _enabled;

        private bool _pathfinding = true;

        private HashSet<ICommonSession> _subscribedSessions = new();

        public override void Initialize()
        {
            base.Initialize();
            UpdatesBefore.Add(typeof(SharedPhysicsSystem));
            InitializeAvoidance();
            _configManager.OnValueChanged(CCVars.NPCEnabled, SetNPCEnabled);
            _configManager.OnValueChanged(CCVars.NPCPathfinding, SetNPCPathfinding);

            SubscribeLocalEvent<NPCSteeringComponent, ComponentShutdown>(OnSteeringShutdown);
            SubscribeNetworkEvent<RequestNPCSteeringDebugEvent>(OnDebugRequest);
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

        private void SetNPCPathfinding(bool value)
        {
            _pathfinding = value;

            if (!_pathfinding)
            {
                foreach (var comp in EntityQuery<NPCSteeringComponent>(true))
                {
                    comp.PathfindToken?.Cancel();
                    comp.PathfindToken = null;
                }
            }
        }

        public override void Shutdown()
        {
            base.Shutdown();
            ShutdownAvoidance();
            _configManager.UnsubValueChanged(CCVars.NPCEnabled, SetNPCEnabled);
        }

        private void OnDebugRequest(RequestNPCSteeringDebugEvent msg, EntitySessionEventArgs args)
        {
            if (!_admin.IsAdmin((IPlayerSession) args.SenderSession))
                return;

            if (msg.Enabled)
                _subscribedSessions.Add(args.SenderSession);
            else
                _subscribedSessions.Remove(args.SenderSession);
        }

        private void OnSteeringShutdown(EntityUid uid, NPCSteeringComponent component, ComponentShutdown args)
        {
            component.PathfindToken?.Cancel();
        }

        /// <summary>
        /// Adds the AI to the steering system to move towards a specific target
        /// </summary>
        public NPCSteeringComponent Register(EntityUid uid, EntityCoordinates coordinates, NPCSteeringComponent? component = null)
        {
            if (Resolve(uid, ref component, false))
            {
                component.PathfindToken?.Cancel();
                component.PathfindToken = null;
                component.CurrentPath.Clear();
            }
            else
            {
                component = AddComp<NPCSteeringComponent>(uid);
                component.Flags = _pathfindingSystem.GetFlags(uid);
            }

            EnsureComp<NPCRVOComponent>(uid);
            component.Coordinates = coordinates;
            return component;
        }

        /// <summary>
        /// Attempts to register the entity. Does nothing if the coordinates already registered.
        /// </summary>
        public bool TryRegister(EntityUid uid, EntityCoordinates coordinates, NPCSteeringComponent? component = null)
        {
            if (Resolve(uid, ref component, false) && component.Coordinates.Equals(coordinates))
            {
                return false;
            }

            Register(uid, coordinates, component);
            return true;
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
            RemComp<NPCRVOComponent>(uid);
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
            var xformQuery = GetEntityQuery<TransformComponent>();

            var npcs = EntityQuery<NPCSteeringComponent, ActiveNPCComponent, InputMoverComponent, TransformComponent>()
                .ToArray();

            // TODO: Do this in parallel.
            // Main obstacle is requesting a new path needs to be done synchronously
            foreach (var (steering, _, mover, xform) in npcs)
            {
                Steer(steering, mover, xform, modifierQuery, bodyQuery, xformQuery, frameTime);
            }

            if (_subscribedSessions.Count > 0)
            {
                var data = new List<NPCSteeringDebugData>(npcs.Length);

                foreach (var (steering, _, mover, _) in npcs)
                {
                    data.Add(new NPCSteeringDebugData(
                        mover.Owner,
                        mover.CurTickSprintMovement,
                        steering.DangerMap,
                        steering.InterestMap));
                }

                var filter = Filter.Empty();
                filter.AddPlayers(_subscribedSessions);

                RaiseNetworkEvent(new NPCSteeringDebugEvent(data), filter);
            }
        }

        private void SetDirection(InputMoverComponent component, NPCSteeringComponent steering, Vector2 value, bool clear = true)
        {
            if (clear && value.Equals(Vector2.Zero))
            {
                steering.CurrentPath.Clear();
            }

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
            EntityQuery<TransformComponent> xformQuery,
            float frameTime)
        {
            if (Deleted(steering.Coordinates.EntityId))
            {
                SetDirection(mover, steering, Vector2.Zero);
                steering.Status = SteeringStatus.NoPath;
                return;
            }

            // No path set from pathfinding or the likes.
            if (steering.Status == SteeringStatus.NoPath)
            {
                SetDirection(mover, steering, Vector2.Zero);
                return;
            }

            // Can't move at all, just noop input.
            if (!mover.CanMove)
            {
                SetDirection(mover, steering, Vector2.Zero);
                steering.Status = SteeringStatus.Moving;
                return;
            }

            var uid = mover.Owner;
            var ourCoordinates = xform.Coordinates;
            var destinationCoordinates = steering.Coordinates;

            // TODO: SHITCODE AAAA
            // TODO: Stackalloc?
            var directions = new Vector2[InterestDirections];
            var interestMap = steering.InterestMap;
            var dangerMap = steering.DangerMap;

            for (var i = 0; i < InterestDirections; i++)
            {
                directions[i] = Angle.FromDegrees(i * (360 / InterestDirections)).ToVec();
                interestMap[i] = 0f;
                dangerMap[i] = 0f;
            }

            var agentRadius = steering.Radius;
            var detectionRadius = 1.5f + agentRadius;
            var (worldPos, worldRot) = xform.GetWorldPositionRotation();

            // Use rotation relative to parent to rotate our context vectors by.
            var offsetRot = worldRot - xform.LocalRotation;

            // TODO: Have some time delay on NPC combat in range before swinging (e.g. 50-200ms) that is a limit.
            // TODO: Have some kind of way to control them avoiding when melee on cd.
            // TODO: Have them hover around some preferred engagement range per-NPC, then they duck out (if target has melee(?))
            // TODO: Have them strafe around the target for some random time then strafe the other direction.

            if (!TrySeek(uid, mover, steering, xform, interestMap, dangerMap, directions, bodyQuery, modifierQuery, frameTime))
            {
                SetDirection(mover, steering, Vector2.Zero);
                return;
            }

            StaticAvoid(uid, offsetRot, worldPos, detectionRadius, agentRadius, xform, dangerMap, directions, bodyQuery, xformQuery);
            // TODO: Avoid anything not considered hostile
            DynamicAvoid(uid);

            var ev = new NPCSteeringEvent(interestMap, dangerMap);
            RaiseLocalEvent(uid, ref ev);

            // Remove the danger map from the interest map.
            for (var i = 0; i < InterestDirections; i++)
            {
                interestMap[i] = Math.Clamp(interestMap[i] - dangerMap[i], 0f, 1f);
            }

            var resultDirection = Vector2.Zero;

            // Get average vector to take.
            for (var i = 0; i < InterestDirections; i++)
            {
                resultDirection += directions[i] * interestMap[i];
            }

            resultDirection = resultDirection == Vector2.Zero ? Vector2.Zero : resultDirection.Normalized;
            SetDirection(mover, steering, resultDirection, false);
        }

        #region Seek

        /// <summary>
        /// Attempts to head to the target destination, either via the next pathfinding node or the final target.
        /// </summary>
        private bool TrySeek(
            EntityUid uid,
            InputMoverComponent mover,
            NPCSteeringComponent steering,
            TransformComponent xform,
            float[] interestMap,
            float[] dangerMap,
            Vector2[] directions,
            EntityQuery<PhysicsComponent> bodyQuery,
            EntityQuery<MovementSpeedModifierComponent> modifierQuery,
            float frameTime)
        {
            var ourCoordinates = xform.Coordinates;
            var destinationCoordinates = steering.Coordinates;

            // We've arrived, nothing else matters.
            if (xform.Coordinates.TryDistance(EntityManager, destinationCoordinates, out var distance) &&
                distance <= steering.Range)
            {
                SetDirection(mover, steering, Vector2.Zero);
                steering.Status = SteeringStatus.InRange;
                return true;
            }

            // Grab the target position, either the next path node or our end goal..
            var targetCoordinates = GetTargetCoordinates(steering);
            var needsPath = false;

            // If the next node is invalid then get new ones
            if (!targetCoordinates.IsValid(EntityManager))
            {
                if (steering.CurrentPath.TryPeek(out var poly) &&
                    (poly.Data.Flags & PathfindingBreadcrumbFlag.Invalid) != 0x0)
                {
                    steering.CurrentPath.Dequeue();
                    // Try to get the next node temporarily.
                    targetCoordinates = GetTargetCoordinates(steering);
                    needsPath = true;
                }
            }

            // Need to be pretty close if it's just a node to make sure LOS for door bashes or the likes.
            float arrivalDistance;

            if (targetCoordinates.Equals(steering.Coordinates))
            {
                // What's our tolerance for arrival.
                // If it's a pathfinding node it might be different to the destination.
                arrivalDistance = steering.Range;
            }
            else
            {
                arrivalDistance = SharedInteractionSystem.InteractionRange - 0.8f;
            }

            // Check if mapids match.
            var targetMap = targetCoordinates.ToMap(EntityManager);
            var ourMap = ourCoordinates.ToMap(EntityManager);

            if (targetMap.MapId != ourMap.MapId)
            {
                steering.Status = SteeringStatus.NoPath;
                return false;
            }

            var direction = targetMap.Position - ourMap.Position;

            // Are we in range
            if (direction.Length <= arrivalDistance)
            {
                // Node needs some kind of special handling like access or smashing.
                if (steering.CurrentPath.TryPeek(out var node))
                {
                    var status = TryHandleFlags(steering, node, bodyQuery);

                    // TODO: Need to handle re-pathing in case the target moves around.
                    switch (status)
                    {
                        case SteeringObstacleStatus.Completed:
                            break;
                        case SteeringObstacleStatus.Failed:
                            // TODO: Blacklist the poly for next query
                            steering.Status = SteeringStatus.NoPath;
                            return false;
                        case SteeringObstacleStatus.Continuing:
                            CheckPath(steering, xform, needsPath, distance);
                            return true;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                // Otherwise it's probably regular pathing so just keep going a bit more to get to tile centre
                if (direction.Length <= TileTolerance)
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
                            SetDirection(mover, steering, Vector2.Zero);
                            steering.Status = SteeringStatus.NoPath;
                            return false;
                        }

                        // Gonna resume now business as usual
                        direction = targetMap.Position - ourMap.Position;
                    }
                    else
                    {
                        // This probably shouldn't happen as we check above but eh.
                        steering.Status = SteeringStatus.NoPath;
                        return false;
                    }
                }
            }

            // Do we have no more nodes to follow OR has the target moved sufficiently? If so then re-path.
            if (!needsPath)
            {
                needsPath = steering.CurrentPath.Count == 0 || (steering.CurrentPath.Peek().Data.Flags & PathfindingBreadcrumbFlag.Invalid) != 0x0;
            }

            // TODO: Probably need partial planning support i.e. patch from the last node to where the target moved to.
            CheckPath(steering, xform, needsPath, distance);

            modifierQuery.TryGetComponent(steering.Owner, out var modifier);
            var moveSpeed = GetSprintSpeed(steering.Owner, modifier);

            var input = direction.Normalized;

            // If we're going to overshoot then... don't.
            // TODO: For tile / movement we don't need to get bang on, just need to make sure we don't overshoot the far end.
            var tickMovement = moveSpeed * frameTime;

            if (tickMovement.Equals(0f))
            {
                steering.Status = SteeringStatus.NoPath;
                return false;
            }

            // We may overshoot slightly but still be in the arrival distance which is okay.
            var maxDistance = direction.Length + arrivalDistance;

            if (tickMovement > maxDistance)
            {
                input *= maxDistance / tickMovement;
            }

            // We have the input in world terms but need to convert it back to what movercontroller is doing.
            input = (-_mover.GetParentGridAngle(mover)).RotateVec(input);
            var norm = input.Normalized;

            for (var i = 0; i < InterestDirections; i++)
            {
                var result = Vector2.Dot(norm, directions[i]);
                interestMap[i] = MathF.Max(interestMap[i], result * input.Length);
            }

            return true;
        }

        private void CheckPath(NPCSteeringComponent steering, TransformComponent xform, bool needsPath, float targetDistance)
        {
            if (!needsPath)
            {
                // If the target has sufficiently moved.
                var lastNode = GetCoordinates(steering.CurrentPath.Last());

                if (lastNode.TryDistance(EntityManager, steering.Coordinates, out var lastDistance) &&
                    lastDistance > steering.RepathRange)
                {
                    needsPath = true;
                }
            }

            // Request the new path.
            if (needsPath)
            {
                RequestPath(steering, xform, targetDistance);
            }
        }

        /// <summary>
        /// We may be pathfinding and moving at the same time in which case early nodes may be out of date.
        /// </summary>
        public void PrunePath(MapCoordinates mapCoordinates, Vector2 direction, Queue<PathPoly> nodes)
        {
            if (nodes.Count == 0)
                return;

            // Prune the first node as it's irrelevant.
            nodes.Dequeue();

            while (nodes.TryPeek(out var node))
            {
                if (!node.Data.IsFreeSpace)
                    break;

                var nodeMap = node.Coordinates.ToMap(EntityManager);

                // If any nodes are 'behind us' relative to the target we'll prune them.
                // This isn't perfect but should fix most cases of stutter stepping.
                if (nodeMap.MapId == mapCoordinates.MapId &&
                    Vector2.Dot(direction, nodeMap.Position - mapCoordinates.Position) < 0f)
                {
                    nodes.Dequeue();
                    continue;
                }

                break;
            }
        }

        /// <summary>
        /// Get the coordinates we should be heading towards.
        /// </summary>
        private EntityCoordinates GetTargetCoordinates(NPCSteeringComponent steering)
        {
            // Depending on what's going on we may return the target or a pathfind node.

            // Even if we're at the last node may not be able to head to target in case we get stuck on a corner or the likes.
            if (steering.CurrentPath.Count >= 1 && steering.CurrentPath.TryPeek(out var nextTarget))
            {
                return GetCoordinates(nextTarget);
            }

            return steering.Coordinates;
        }

        #endregion

        #region Static Avoidance

        /// <summary>
        /// Tries to avoid static blockers such as walls.
        /// </summary>
        private void StaticAvoid(
            EntityUid uid,
            Angle offsetRot,
            Vector2 worldPos,
            float detectionRadius,
            float agentRadius,
            TransformComponent xform,
            float[] dangerMap,
            Vector2[] directions,
            EntityQuery<PhysicsComponent> bodyQuery,
            EntityQuery<TransformComponent> xformQuery)
        {
            // Static Avoidance (TODO: METHOD)
            foreach (var ent in _lookup.GetEntitiesInRange(uid, detectionRadius, LookupFlags.Static))
            {
                // TODO: If we can access the door or smth.
                if (ent == uid ||
                    !bodyQuery.TryGetComponent(ent, out var otherBody) ||
                    !otherBody.CanCollide ||
                    !otherBody.Hard)
                {
                    // TODO: Mask
                    continue;
                }

                // TODO: More queries and shit.
                if (!_physics.TryGetNearestPoints(uid, ent, out var pointA, out var pointB, xform, xformQuery.GetComponent(ent)))
                {
                    continue;
                }

                var obstacleDirection = offsetRot.RotateVec(pointB - worldPos);
                var obstacleDistance = obstacleDirection.Length;

                if (obstacleDistance > detectionRadius)
                    continue;

                var weight = obstacleDistance <= agentRadius ? 1f : (detectionRadius - obstacleDistance) / detectionRadius;
                var norm = obstacleDirection.Normalized;

                for (var i = 0; i < InterestDirections; i++)
                {
                    var result = Vector2.Dot(norm, directions[i]);
                    var inputValue = result * weight;
                    dangerMap[i] = MathF.Max(inputValue, dangerMap[i]);
                }
            }
        }

        #endregion

        #region Dynamic Avoidance

        private void DynamicAvoid(EntityUid uid)
        {

        }

        #endregion

        private EntityCoordinates GetCoordinates(PathPoly poly)
        {
            if (!poly.IsValid())
                return EntityCoordinates.Invalid;

            return new EntityCoordinates(poly.GraphUid, poly.Box.Center);
        }

        /// <summary>
        /// Get a new job from the pathfindingsystem
        /// </summary>
        private async void RequestPath(NPCSteeringComponent steering, TransformComponent xform, float targetDistance)
        {
            // If we already have a pathfinding request then don't grab another.
            // If we're in range then just beeline them; this can avoid stutter stepping and is an easy way to look nicer.
            if (steering.Pathfind || targetDistance < steering.RepathRange)
                return;

            steering.PathfindToken = new CancellationTokenSource();

            var flags = _pathfindingSystem.GetFlags(steering.Owner);

            var result = await _pathfindingSystem.GetPath(
                steering.Owner,
                xform.Coordinates,
                steering.Coordinates,
                steering.Range,
                steering.PathfindToken.Token,
                flags);

            if (result.Result == PathResult.NoPath)
            {
                steering.CurrentPath.Clear();
                steering.PathfindToken = null;
                steering.FailedPathCount++;

                if (steering.FailedPathCount >= NPCSteeringComponent.FailedPathLimit)
                {
                    steering.Status = SteeringStatus.NoPath;
                }

                return;
            }

            var targetPos = steering.Coordinates.ToMap(EntityManager);
            var ourPos = xform.MapPosition;

            PrunePath(ourPos, targetPos.Position - ourPos.Position, result.Path);
            steering.CurrentPath = result.Path;
            steering.PathfindToken = null;
        }

        // TODO: Move these to movercontroller

        private float GetSprintSpeed(EntityUid uid, MovementSpeedModifierComponent? modifier = null)
        {
            if (!Resolve(uid, ref modifier, false))
            {
                return MovementSpeedModifierComponent.DefaultBaseSprintSpeed;
            }

            return modifier.CurrentSprintSpeed;
        }
    }
}
