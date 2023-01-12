using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
using Robust.Shared.Threading;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.NPC.Systems
{
    public sealed partial class NPCSteeringSystem : SharedNPCSteeringSystem
    {
        /*
         * We use context steering to determine which way to move.
         * This involves creating an array of possible directions and assigning a value for the desireability of each direction.
         *
         * There's multiple ways to implement this, e.g. you can average all directions, or you can choose the highest direction
         * , or you can remove the danger map entirely and only having an interest map (AKA game endeavour).
         * See http://www.gameaipro.com/GameAIPro2/GameAIPro2_Chapter18_Context_Steering_Behavior-Driven_Steering_at_the_Macro_Scale.pdf
         * (though in their case it was for an F1 game so used context steering across the width of the road).
         */

        [Dependency] private readonly IAdminManager _admin = default!;
        [Dependency] private readonly IConfigurationManager _configManager = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IParallelManager _parallel = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly DoorSystem _doors = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly FactionSystem _faction = default!;
        // [Dependency] private readonly MetaDataSystem _metadata = default!;
        [Dependency] private readonly PathfindingSystem _pathfindingSystem = default!;
        [Dependency] private readonly SharedInteractionSystem _interaction = default!;
        [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
        [Dependency] private readonly SharedMoverController _mover = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;

        // This will likely get moved onto an abstract pathfinding node that specifies the max distance allowed from the coordinate.
        private const float TileTolerance = 0.40f;

        private bool _enabled;

        private bool _pathfinding = true;

        public static readonly Vector2[] Directions = new Vector2[InterestDirections];

        private readonly HashSet<ICommonSession> _subscribedSessions = new();

        private object _obstacles = new();

        public override void Initialize()
        {
            base.Initialize();

            for (var i = 0; i < InterestDirections; i++)
            {
                Directions[i] = new Angle(InterestRadians * i).ToVec();
            }

            UpdatesBefore.Add(typeof(SharedPhysicsSystem));
            _configManager.OnValueChanged(CCVars.NPCEnabled, SetNPCEnabled, true);
            _configManager.OnValueChanged(CCVars.NPCPathfinding, SetNPCPathfinding, true);

            SubscribeLocalEvent<NPCSteeringComponent, ComponentShutdown>(OnSteeringShutdown);
            SubscribeNetworkEvent<RequestNPCSteeringDebugEvent>(OnDebugRequest);
        }

        private void SetNPCEnabled(bool obj)
        {
            if (!obj)
            {
                foreach (var (comp, mover) in EntityQuery<NPCSteeringComponent, InputMoverComponent>())
                {
                    mover.CurTickSprintMovement = Vector2.Zero;
                    comp.PathfindToken?.Cancel();
                    comp.PathfindToken = null;
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
            _configManager.UnsubValueChanged(CCVars.NPCEnabled, SetNPCEnabled);
            _configManager.UnsubValueChanged(CCVars.NPCPathfinding, SetNPCPathfinding);
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
            // Cancel any active pathfinding jobs as they're irrelevant.
            component.PathfindToken?.Cancel();
            component.PathfindToken = null;
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

            var npcs = EntityQuery<ActiveNPCComponent, NPCSteeringComponent, InputMoverComponent, TransformComponent>()
                .ToArray();
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = _parallel.ParallelProcessCount,
            };

            Parallel.For(0, npcs.Length, options, i =>
            {
                var (_, steering, mover, xform) = npcs[i];

                Steer(steering, mover, xform, modifierQuery, bodyQuery, xformQuery, frameTime);
            });


            if (_subscribedSessions.Count > 0)
            {
                var data = new List<NPCSteeringDebugData>(npcs.Length);

                foreach (var (_, steering, mover, _) in npcs)
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

            /* If you wish to not steer every tick A) Add pause support B) fix overshoots to prevent dancing
            var nextSteer = steering.LastTimeSteer + TimeSpan.FromSeconds(1f / NPCSteeringComponent.SteerFrequency);

            if (nextSteer > _timing.CurTime)
            {
                SetDirection(mover, steering, steering.LastSteer, false);
                return;
            }
            */

            var uid = mover.Owner;
            var interest = steering.Interest;
            var danger = steering.Danger;
            var agentRadius = steering.Radius;
            var worldPos = xform.WorldPosition;
            var (layer, mask) = _physics.GetHardCollision(uid);

            // Use rotation relative to parent to rotate our context vectors by.
            var offsetRot = -_mover.GetParentGridAngle(mover);
            modifierQuery.TryGetComponent(uid, out var modifier);
            var moveSpeed = GetSprintSpeed(steering.Owner, modifier);
            var body = bodyQuery.GetComponent(uid);
            var dangerPoints = steering.DangerPoints;
            dangerPoints.Clear();

            for (var i = 0; i < InterestDirections; i++)
            {
                steering.Interest[i] = 0f;
                steering.Danger[i] = 0f;
            }

            var ev = new NPCSteeringEvent(steering, interest, danger, agentRadius, offsetRot, worldPos);
            RaiseLocalEvent(uid, ref ev);

            if (steering.CanSeek && !TrySeek(uid, mover, steering, body, xform, offsetRot, moveSpeed, interest, bodyQuery,  frameTime))
            {
                SetDirection(mover, steering, Vector2.Zero);
                return;
            }
            DebugTools.Assert(!float.IsNaN(interest[0]));

            // Avoid static objects like walls
            CollisionAvoidance(uid, offsetRot, worldPos, agentRadius, moveSpeed, layer, mask, xform, danger, dangerPoints, bodyQuery, xformQuery);
            DebugTools.Assert(!float.IsNaN(danger[0]));

            Separation(uid, offsetRot, worldPos, agentRadius, layer, mask, body, xform, danger, bodyQuery, xformQuery);

            // Remove the danger map from the interest map.
            var desiredDirection = -1;
            var desiredValue = 0f;

            for (var i = 0; i < InterestDirections; i++)
            {
                var adjustedValue = Math.Clamp(interest[i] - danger[i], 0f, 1f);

                if (adjustedValue > desiredValue)
                {
                    desiredDirection = i;
                    desiredValue = adjustedValue;
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

            var result = await _pathfindingSystem.GetPathSafe(
                steering.Owner,
                xform.Coordinates,
                steering.Coordinates,
                steering.Range,
                steering.PathfindToken.Token,
                flags);

            steering.PathfindToken = null;

            if (result.Result == PathResult.NoPath)
            {
                steering.CurrentPath.Clear();
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
