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
using Robust.Shared.Utility;

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
        [Dependency] private readonly FactionSystem _faction = default!;
        [Dependency] private readonly MetaDataSystem _metadata = default!;
        [Dependency] private readonly PathfindingSystem _pathfindingSystem = default!;
        [Dependency] private readonly SharedInteractionSystem _interaction = default!;
        [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
        [Dependency] private readonly SharedMoverController _mover = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;

        // This will likely get moved onto an abstract pathfinding node that specifies the max distance allowed from the coordinate.
        private const float TileTolerance = 0.40f;

        private bool _enabled;

        private bool _pathfinding = true;

        private static readonly Vector2[] Directions = new Vector2[InterestDirections];

        private readonly HashSet<ICommonSession> _subscribedSessions = new();

        public override void Initialize()
        {
            base.Initialize();

            for (var i = 0; i < InterestDirections; i++)
            {
                Directions[i] = new Angle(InterestRadians * i).ToVec();
            }

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
                steering.LastSteer = mover.CurTickSprintMovement;
            }

            if (_subscribedSessions.Count > 0)
            {
                var data = new List<NPCSteeringDebugData>(npcs.Length);

                foreach (var (steering, _, mover, _) in npcs)
                {
                    data.Add(new NPCSteeringDebugData(
                        mover.Owner,
                        mover.CurTickSprintMovement,
                        steering.Interest,
                        steering.Danger,
                        steering.DangerPoints));
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
                steering.Status = SteeringStatus.NoPath;
                return;
            }

            // TODO: Pause time
            var nextSteer = steering.LastTimeSteer + TimeSpan.FromSeconds(1f / NPCSteeringComponent.SteerFrequency);

            if (nextSteer > _timing.CurTime)
            {
                SetDirection(mover, steering, steering.LastSteer, false);
                return;
            }

            steering.LastTimeSteer = _timing.CurTime;
            var uid = mover.Owner;
            var interest = steering.Interest;
            var danger = steering.Danger;
            var agentRadius = steering.Radius;
            var (worldPos, worldRot) = xform.GetWorldPositionRotation();

            // Use rotation relative to parent to rotate our context vectors by.
            var offsetRot = -_mover.GetParentGridAngle(mover);
            // TODO: AAA
            var moveSpeed = GetSprintSpeed(steering.Owner);
            var tickMove = moveSpeed * frameTime;
            var body = bodyQuery.GetComponent(uid);
            var dangerPoints = steering.DangerPoints;
            dangerPoints.Clear();

            for (var i = 0; i < InterestDirections; i++)
            {
                steering.Interest[i] = 0f;
                steering.Danger[i] = 0f;
            }

            // TODO: Have some time delay on NPC combat in range before swinging (e.g. 50-200ms) that is a limit.
            // TODO: Have some kind of way to control them avoiding when melee on cd.
            // TODO: Have them hover around some preferred engagement range per-NPC, then they duck out (if target has melee(?))
            // TODO: Have them strafe around the target for some random time then strafe the other direction.

            if (!TrySeek(uid, mover, steering, body, xform, offsetRot, interest, bodyQuery, modifierQuery, frameTime))
            {
                SetDirection(mover, steering, Vector2.Zero);
                return;
            }
            DebugTools.Assert(!float.IsNaN(interest[0]));

            // Avoid static objects like walls
            StaticAvoid(uid, offsetRot, agentRadius, body, xform, danger, dangerPoints, bodyQuery, xformQuery);
            DebugTools.Assert(!float.IsNaN(danger[0]));

            Separation(uid, worldPos, agentRadius, body, xform, interest, bodyQuery, xformQuery);

            // TODO: Refs
            var ev = new NPCSteeringEvent(Directions, interest, danger, agentRadius, offsetRot, worldPos);
            RaiseLocalEvent(uid, ref ev);
            var adjustedInterestMap = new float[InterestDirections];

            // Remove the danger map from the interest map.
            var desiredDirection = -1;
            var desiredValue = 0f;

            for (var i = 0; i < InterestDirections; i++)
            {
                adjustedInterestMap[i] = Math.Clamp(interest[i] - danger[i], 0f, 1f);

                if (adjustedInterestMap[i] > desiredValue)
                {
                    desiredDirection = i;
                    desiredValue = adjustedInterestMap[i];
                }
            }

            var resultDirection = Vector2.Zero;

            if (desiredDirection != -1)
            {
                resultDirection = new Angle(desiredDirection * InterestRadians).ToVec();
            }

            DebugTools.Assert(!float.IsNaN(resultDirection.X));
            SetDirection(mover, steering, resultDirection, false);
        }

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

            // Short-circuit with no path.
            var targetPoly = _pathfindingSystem.GetPoly(steering.Coordinates);

            if (targetPoly != null && steering.Coordinates.Position.Equals(Vector2.Zero) && _interaction.InRangeUnobstructed(steering.Owner, steering.Coordinates.EntityId))
            {
                steering.CurrentPath.Clear();
                steering.CurrentPath.Enqueue(targetPoly);
                return;
            }

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
