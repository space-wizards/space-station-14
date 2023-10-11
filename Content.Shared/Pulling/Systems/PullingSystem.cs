using System.Diagnostics.CodeAnalysis;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Database;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Players;
using Robust.Shared.Timing;

namespace Content.Shared.Movement.Pulling.Systems;

/// <summary>
/// Allows one entity to pull another behind them via a physics distance joint.
/// </summary>
public sealed class PullingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly SharedJointSystem _joints = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _xformSys = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(SharedPhysicsSystem));
        UpdatesOutsidePrediction = true;

        SubscribeLocalEvent<PullableComponent, MoveInputEvent>(OnPullableMoveInput);
        SubscribeLocalEvent<PullableComponent, CollisionChangeEvent>(OnPullableCollisionChange);
        SubscribeLocalEvent<PullableComponent, JointRemovedEvent>(OnJointRemoved);
        SubscribeLocalEvent<PullableComponent, GetVerbsEvent<Verb>>(AddPullVerbs);

        SubscribeLocalEvent<PullerComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        SubscribeLocalEvent<PullerComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.MovePulledObject, new PointerInputCmdHandler(OnRequestMovePulledObject))
            .Register<PullingSystem>();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ReleasePulledObject, InputCmdHandler.FromDelegate(OnReleasePulledObject))
            .Register<PullingSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<PullingSystem>();
    }

    private void OnVirtualItemDeleted(EntityUid uid, PullerComponent component, VirtualItemDeletedEvent args)
    {
        // If client deletes the virtual hand then stop the pull.
        if (component.Pulling == null)
            return;

        if (component.Pulling == args.BlockingEntity)
        {
            if (EntityManager.TryGetComponent<PullableComponent>(args.BlockingEntity, out var comp))
            {
                TryStopPull(comp, uid);
            }
        }
    }

    private void AddPullVerbs(EntityUid uid, PullableComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // Are they trying to pull themselves up by their bootstraps?
        if (args.User == args.Target)
            return;

        //TODO VERB ICONS add pulling icon
        if (component.Puller == args.User)
        {
            Verb verb = new()
            {
                Text = Loc.GetString("pulling-verb-get-data-text-stop-pulling"),
                Act = () => TryStopPull(component, args.User),
                DoContactInteraction = false // pulling handle its own contact interaction.
            };
            args.Verbs.Add(verb);
        }
        else if (CanPull(args.User, args.Target))
        {
            Verb verb = new()
            {
                Text = Loc.GetString("pulling-verb-get-data-text"),
                Act = () => TryStartPull(args.User, args.Target),
                DoContactInteraction = false // pulling handle its own contact interaction.
            };
            args.Verbs.Add(verb);
        }
    }

    private void OnRefreshMovespeed(EntityUid uid, PullerComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.WalkSpeedModifier, component.SprintSpeedModifier);
    }

    private void OnPullableMoveInput(EntityUid uid, PullableComponent component, ref MoveInputEvent args)
    {
        // If someone moves then break their pulling.
        if (!component.BeingPulled)
            return;

        var entity = args.Entity;

        if (!_blocker.CanMove(entity))
            return;

        TryStopPull(component);
    }

    private void OnPullableCollisionChange(EntityUid uid, PullableComponent component, ref CollisionChangeEvent args)
    {
        if (component.PullJointId != null && !args.CanCollide)
        {
            _joints.RemoveJoint(uid, component.PullJointId);
        }
    }

    private void OnJointRemoved(EntityUid uid, PullableComponent component, JointRemovedEvent args)
    {
        // Not relevant / pullable state handle it.
        if (component.Puller != args.OtherEntity ||
            args.Joint.ID != component.PullJointId ||
            _timing.ApplyingState)
        {
            return;
        }

        // Do we have some other join with our Puller?
        // or alternatively:
        // TODO track the relevant joint.
        if (args.Joint.ID != component.PullJointId)
            return;

        // No more joints with puller -> force stop pull.
        InternalRemovePulling(uid, component.Puller.Value, component);
    }

    public bool IsPulled(EntityUid uid, PullableComponent? component = null)
    {
        return Resolve(uid, ref component, false) && component.BeingPulled;
    }

    private bool OnRequestMovePulledObject(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        if (session?.AttachedEntity is not { } player ||
            !player.IsValid())
        {
            return false;
        }

        if (!TryComp<PullerComponent>(player, out var pullerComp))
            return false;

        var pulled = pullerComp.Pulling;

        if (!HasComp<PullableComponent>(pulled))
            return false;

        if (_containerSystem.IsEntityInContainer(player))
            return false;

        // Cap the distance
        const float range = 2f;
        var fromUserCoords = coords.WithEntityId(player, EntityManager);
        var userCoords = Transform(player).Coordinates;

        if (!userCoords.InRange(EntityManager, _xformSys, fromUserCoords, range))
        {
            var userDirection = fromUserCoords.Position - userCoords.Position;
            fromUserCoords = userCoords.Offset(userDirection.Normalized() * range);
        }

        _throwing.TryThrow(pulled.Value, fromUserCoords, user: player, playSound: false);
        return false;
    }

    /// <summary>
    /// Cleans up pulling state without touching the joint.
    /// </summary>
    private void InternalRemovePulling(EntityUid pullable, EntityUid puller,
        PullableComponent? pullableComp = null,
        PullerComponent? pullerComp = null)
    {

    }

    public bool IsPulling(EntityUid puller, PullerComponent? component = null)
    {
        return Resolve(puller, ref component, false) && component.Pulling != null;
    }

    private void OnReleasePulledObject(ICommonSession? session)
    {
        if (session?.AttachedEntity is not {Valid: true} player)
        {
            return;
        }

        if (!TryGetPulled(player, out var pulled))
        {
            return;
        }

        if (!EntityManager.TryGetComponent(pulled.Value, out PullableComponent? pullable))
        {
            return;
        }

        TryStopPull(pullable);
    }

    public bool CanPull(EntityUid puller, EntityUid pulled)
    {
        if (!EntityManager.TryGetComponent<PullerComponent>(puller, out var comp))
        {
            return false;
        }

        if (comp.NeedsHands && !_handsSystem.TryGetEmptyHand(puller, out _))
        {
            return false;
        }

        if (!_blocker.CanInteract(puller, pulled))
        {
            return false;
        }

        if (!EntityManager.TryGetComponent<PhysicsComponent>(pulled, out var physics))
        {
            return false;
        }

        if (physics.BodyType == BodyType.Static)
        {
            return false;
        }

        if (puller == pulled)
        {
            return false;
        }

        if (!_containerSystem.IsInSameOrNoContainer(puller, pulled))
        {
            return false;
        }

        if (EntityManager.TryGetComponent(puller, out BuckleComponent? buckle))
        {
            // Prevent people pulling the chair they're on, etc.
            if (buckle is { PullStrap: false, Buckled: true } && (buckle.LastEntityBuckledTo == pulled))
            {
                return false;
            }
        }

        var getPulled = new BeingPulledAttemptEvent(puller, pulled);
        RaiseLocalEvent(pulled, getPulled, true);
        var startPull = new StartPullAttemptEvent(puller, pulled);
        RaiseLocalEvent(puller, startPull, true);
        return (!startPull.Cancelled && !getPulled.Cancelled);
    }

    public bool TogglePull(EntityUid puller, PullableComponent pullable)
    {
        if (pullable.Puller == puller)
        {
            return TryStopPull(pullable);
        }
        return TryStartPull(puller, pullable.Owner);
    }

    public bool TryStopPull(EntityUid pullableUid, PullableComponent pullable, EntityUid? user = null)
    {
        if (!pullable.BeingPulled)
            return false;

        var msg = new AttemptStopPullingEvent(user);
        RaiseLocalEvent(pullable.Owner, msg, true);

        if (msg.Cancelled)
            return false;

        // Stop pulling confirmed!

        if (TryComp<PhysicsComponent>(pullable.Owner, out var pullablePhysics))
        {
            _physics.SetFixedRotation(pullable.Owner, pullable.PrevFixedRotation, body: pullablePhysics);
        }

        var pullerPhysics = EntityManager.GetComponent<PhysicsComponent>(puller.Owner);
        var pullablePhysics = EntityManager.GetComponent<PhysicsComponent>(pullable.Owner);

        // MovingTo shutdown
        ForceSetMovingTo(pullable, null);

        // Joint shutdown
        if (!_timing.ApplyingState && // During state-handling, joint component will handle its own state.
            pullable.PullJointId != null &&
            TryComp(puller.Owner, out JointComponent? jointComp))
        {
            if (jointComp.GetJoints.TryGetValue(pullable.PullJointId, out var j))
                _joints.RemoveJoint(j);
        }
        pullable.PullJointId = null;

        // State shutdown
        puller.Pulling = null;
        pullable.Puller = null;

        // Messaging
        var message = new PullStoppedMessage(pullerPhysics, pullablePhysics);
        _alertsSystem.ClearAlert(pullerPhysics.Owner, AlertType.Pulling);
        _alertsSystem.ClearAlert(pullable.Owner, AlertType.Pulled);
        RefreshMovementSpeed(component);
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(euid):user} stopped pulling {ToPrettyString(args.Pulled.Owner):target}");

        RaiseLocalEvent(puller.Owner, message, broadcast: false);

        if (Initialized(pullable.Owner))
            RaiseLocalEvent(pullable.Owner, message, true);

        // Networking
        Dirty(puller);
        Dirty(pullable);
        return true;
    }

    public bool TryStartPull(EntityUid puller, EntityUid pullable, PullerComponent? pullerComp = null, PullableComponent? pullableComp = null)
    {
        if (!Resolve(puller, ref pullerComp, false) ||
            !Resolve(pullable, ref pullableComp, false))
        {
            return false;
        }

        if (puller.Pulling == pullable.Owner)
            return true;

        // Pulling a new object : Perform sanity checks.

        if (!CanPull(puller.Owner, pullable.Owner))
        {
            return false;
        }

        if (!EntityManager.TryGetComponent<PhysicsComponent>(puller.Owner, out var pullerPhysics))
        {
            return false;
        }

        if (!EntityManager.TryGetComponent<PhysicsComponent>(pullable.Owner, out var pullablePhysics))
        {
            return false;
        }

        // Ensure that the puller is not currently pulling anything.
        // If this isn't done, then it happens too late, and the start/stop messages go out of order,
        //  and next thing you know it thinks it's not pulling anything even though it is!

        var oldPullable = puller.Pulling;
        if (oldPullable != null)
        {
            if (EntityManager.TryGetComponent(oldPullable.Value, out PullableComponent? oldPullableComp))
            {
                if (!TryStopPull(oldPullableComp))
                {
                    return false;
                }
            }
            else
            {
                Log.Warning("Well now you've done it, haven't you? Someone transferred pulling (onto {0}) while presently pulling something that has no Pullable component (on {1})!", pullable.Owner, oldPullable);
                return false;
            }
        }

        // Ensure that the pullable is not currently being pulled.
        // Same sort of reasons as before.

        var oldPuller = pullable.Puller;
        if (oldPuller != null)
        {
            if (!TryStopPull(pullable))
            {
                return false;
            }
        }

        // Continue with pulling process.

        var pullAttempt = new PullAttemptEvent(pullerPhysics, pullablePhysics);

        RaiseLocalEvent(puller.Owner, pullAttempt, broadcast: false);

        if (pullAttempt.Cancelled)
        {
            return false;
        }

        RaiseLocalEvent(pullable.Owner, pullAttempt, true);

        if (pullAttempt.Cancelled)
            return false;

        _interaction.DoContactInteraction(pullable.Owner, puller.Owner);

        if (pullable != null && puller != null && (puller.Pulling == pullable.Owner))
        {
            // Already done
            return;
        }

        // Start by disconnecting the pullable from whatever it is currently connected to.
        var pullableOldPullerE = pullable?.Puller;
        if (pullableOldPullerE != null)
        {
            ForceDisconnect(EntityManager.GetComponent<PullerComponent>(pullableOldPullerE.Value), pullable!);
        }

        // Continue with the puller.
        var pullerOldPullableE = puller?.Pulling;
        if (pullerOldPullableE != null)
        {
            ForceDisconnect(puller!, EntityManager.GetComponent<PullableComponent>(pullerOldPullableE.Value));
        }

        // And now for the actual connection (if any).

        if (puller != null && pullable != null)
        {
            var pullerPhysics = EntityManager.GetComponent<PhysicsComponent>(puller.Owner);
            var pullablePhysics = EntityManager.GetComponent<PhysicsComponent>(pullable.Owner);
            pullable.PullJointId = $"pull-joint-{pullable.Owner}";

            // State startup
            puller.Pulling = pullable.Owner;
            pullable.Puller = puller.Owner;

            // joint state handling will manage its own state
            if (!_timing.ApplyingState)
            {
                // Joint startup
                var union = _physics.GetHardAABB(puller.Owner).Union(_physics.GetHardAABB(pullable.Owner, body: pullablePhysics));
                var length = Math.Max((float) union.Size.X, (float) union.Size.Y) * 0.75f;

                var joint = _jointSystem.CreateDistanceJoint(pullablePhysics.Owner, pullerPhysics.Owner, id: pullable.PullJointId);
                joint.CollideConnected = false;
                // This maximum has to be there because if the object is constrained too closely, the clamping goes backwards and asserts.
                joint.MaxLength = Math.Max(1.0f, length);
                joint.Length = length * 0.75f;
                joint.MinLength = 0f;
                joint.Stiffness = 1f;
            }

            // Messaging
            var message = new PullStartedMessage(pullerPhysics, pullablePhysics);
            _alertsSystem.ShowAlert(pullerPhysics.Owner, AlertType.Pulling);
            _alertsSystem.ShowAlert(pullable.Owner, AlertType.Pulled);

            RaiseLocalEvent(puller.Owner, message, broadcast: false);
            RaiseLocalEvent(pullable.Owner, message, true);

            Dirty(puller);
            Dirty(pullable);
        }
        pullable.PrevFixedRotation = pullablePhysics.FixedRotation;
        _physics.SetFixedRotation(pullable.Owner, pullable.FixedRotationOnPull, body: pullablePhysics);
        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(puller.Owner):user} started pulling {ToPrettyString(pullable.Owner):target}");
        return true;
    }
}
