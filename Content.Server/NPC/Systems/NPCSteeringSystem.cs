using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.DoAfter;
using Content.Server.Doors.Systems;
using Content.Server.NPC.Components;
using Content.Server.NPC.Events;
using Content.Server.NPC.Pathfinding;
using Content.Shared.CCVar;
using Content.Shared.CombatMode;
using Content.Shared.Interaction;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.NPC;
using Content.Shared.NPC.Events;
using Content.Shared.Physics;
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

namespace Content.Server.NPC.Systems;

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
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly DoorSystem _doors = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly FactionSystem _faction = default!;
    [Dependency] private readonly PathfindingSystem _pathfindingSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;

    /// <summary>
    /// Enabled antistuck detection so if an NPC is in the same spot for a while it will re-path.
    /// </summary>
    public bool AntiStuck = true;

    private bool _enabled;

    private bool _pathfinding = true;

    public static readonly Vector2[] Directions = new Vector2[InterestDirections];

    private readonly HashSet<ICommonSession> _subscribedSessions = new();

    private object _obstacles = new();

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("npc.steering");
#if DEBUG
        _sawmill.Level = LogLevel.Warning;
#else
            _sawmill.Level = LogLevel.Debug;
#endif

        for (var i = 0; i < InterestDirections; i++)
        {
            Directions[i] = new Angle(InterestRadians * i).ToVec();
        }

        UpdatesBefore.Add(typeof(SharedPhysicsSystem));
        _configManager.OnValueChanged(CCVars.NPCEnabled, SetNPCEnabled, true);
        _configManager.OnValueChanged(CCVars.NPCPathfinding, SetNPCPathfinding, true);

        SubscribeLocalEvent<NPCSteeringComponent, ComponentShutdown>(OnSteeringShutdown);
        SubscribeLocalEvent<NPCSteeringComponent, EntityUnpausedEvent>(OnSteeringUnpaused);
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

    private void OnSteeringUnpaused(EntityUid uid, NPCSteeringComponent component, ref EntityUnpausedEvent args)
    {
        component.LastStuckTime += args.PausedTime;
        component.NextSteer += args.PausedTime;
    }

    /// <summary>
    /// Adds the AI to the steering system to move towards a specific target
    /// </summary>
    public NPCSteeringComponent Register(EntityUid uid, EntityCoordinates coordinates, NPCSteeringComponent? component = null)
    {
        if (Resolve(uid, ref component, false))
        {
            if (component.Coordinates.Equals(coordinates))
                return component;

            component.PathfindToken?.Cancel();
            component.PathfindToken = null;
            component.CurrentPath.Clear();
        }
        else
        {
            component = AddComp<NPCSteeringComponent>(uid);
            component.Flags = _pathfindingSystem.GetFlags(uid);
        }

        ResetStuck(component, Transform(uid).Coordinates);
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

        if (EntityManager.TryGetComponent(uid, out InputMoverComponent? controller))
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
            .Select(o => (o.Item1.Owner, o.Item2, o.Item3, o.Item4)).ToArray();

        // Dependency issues across threads.
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = 1,
        };
        var curTime = _timing.CurTime;

        Parallel.For(0, npcs.Length, options, i =>
        {
            var (uid, steering, mover, xform) = npcs[i];
            Steer(uid, steering, mover, xform, modifierQuery, bodyQuery, xformQuery, frameTime, curTime);
        });


        if (_subscribedSessions.Count > 0)
        {
            var data = new List<NPCSteeringDebugData>(npcs.Length);

            foreach (var (uid, steering, mover, _) in npcs)
            {
                data.Add(new NPCSteeringDebugData(
                    uid,
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
        EntityUid uid,
        NPCSteeringComponent steering,
        InputMoverComponent mover,
        TransformComponent xform,
        EntityQuery<MovementSpeedModifierComponent> modifierQuery,
        EntityQuery<PhysicsComponent> bodyQuery,
        EntityQuery<TransformComponent> xformQuery,
        float frameTime,
        TimeSpan curTime)
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

        var interest = steering.Interest;
        var danger = steering.Danger;
        var agentRadius = steering.Radius;
        var worldPos = _transform.GetWorldPosition(xform, xformQuery);
        var (layer, mask) = _physics.GetHardCollision(uid);

        // Use rotation relative to parent to rotate our context vectors by.
        var offsetRot = -_mover.GetParentGridAngle(mover);
        modifierQuery.TryGetComponent(uid, out var modifier);
        var moveSpeed = GetSprintSpeed(uid, modifier);
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
        // If seek has arrived at the target node for example then immediately re-steer.
        var forceSteer = true;

        if (steering.CanSeek && !TrySeek(uid, mover, steering, body, xform, offsetRot, moveSpeed, interest, bodyQuery, frameTime, ref forceSteer))
        {
            SetDirection(mover, steering, Vector2.Zero);
            return;
        }
        DebugTools.Assert(!float.IsNaN(interest[0]));

        // Avoid static objects like walls
        CollisionAvoidance(uid, offsetRot, worldPos, agentRadius, layer, mask, xform, danger, dangerPoints, bodyQuery, xformQuery);
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

        // Don't steer too frequently to avoid twitchiness.
        // This should also implicitly solve tie situations.
        // I think doing this after all the ops above is best?
        // Originally I had it way above but sometimes mobs would overshoot their tile targets.

        if (!forceSteer && steering.NextSteer > curTime)
        {
            SetDirection(mover, steering, steering.LastSteerDirection, false);
            return;
        }

        steering.NextSteer = curTime + TimeSpan.FromSeconds(1f / NPCSteeringComponent.SteeringFrequency);
        steering.LastSteerDirection = resultDirection;
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
    private async void RequestPath(EntityUid uid, NPCSteeringComponent steering, TransformComponent xform, float targetDistance)
    {
        // If we already have a pathfinding request then don't grab another.
        // If we're in range then just beeline them; this can avoid stutter stepping and is an easy way to look nicer.
        if (steering.Pathfind || targetDistance < steering.RepathRange)
            return;

        // Short-circuit with no path.
        var targetPoly = _pathfindingSystem.GetPoly(steering.Coordinates);

        // If this still causes issues future sloth adjust the collision mask.
        // Thanks past sloth I already realised.
        if (targetPoly != null &&
            steering.Coordinates.Position.Equals(Vector2.Zero) &&
            TryComp<PhysicsComponent>(uid, out var physics) &&
            _interaction.InRangeUnobstructed(uid, steering.Coordinates.EntityId, range: 30f, (CollisionGroup) physics.CollisionMask))
        {
            steering.CurrentPath.Clear();
            // Enqueue our poly as it will be pruned later.
            var ourPoly = _pathfindingSystem.GetPoly(xform.Coordinates);

            if (ourPoly != null)
            {
                steering.CurrentPath.Enqueue(ourPoly);
            }

            steering.CurrentPath.Enqueue(targetPoly);
            return;
        }

        steering.PathfindToken = new CancellationTokenSource();

        var flags = _pathfindingSystem.GetFlags(uid);

        var result = await _pathfindingSystem.GetPathSafe(
            uid,
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

        var targetPos = steering.Coordinates.ToMap(EntityManager, _transform);
        var ourPos = xform.MapPosition;

        PrunePath(uid, ourPos, targetPos.Position - ourPos.Position, result.Path);
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
