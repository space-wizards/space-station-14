using System.Diagnostics.CodeAnalysis;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Buckle.Components;
using Content.Shared.Database;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics.Pull;
using Content.Shared.Pulling.Events;
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
    [Dependency] private readonly MovementSpeedModifierSystem _modifierSystem = default!;
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
        return !startPull.Cancelled && !getPulled.Cancelled;
    }

    public bool TogglePull(EntityUid puller, PullableComponent pullable)
    {
        if (pullable.Puller == puller)
        {
            return TryStopPull(pullable);
        }
        return TryStartPull(puller, pullable.Owner);
    }

    public bool TryStartPull(EntityUid pullerUid, EntityUid pullableUid, EntityUid? user = null,
        PullerComponent? pullerComp = null, PullableComponent? pullableComp = null)
    {
        if (!Resolve(pullerUid, ref pullerComp, false) ||
            !Resolve(pullableUid, ref pullableComp, false))
        {
            return false;
        }

        if (pullerComp.Pulling == pullableUid)
            return true;

        if (!CanPull(pullerUid, pullableUid))
        {
            return false;
        }

        if (!EntityManager.TryGetComponent<PhysicsComponent>(pullerUid, out var pullerPhysics))
        {
            return false;
        }

        if (!EntityManager.TryGetComponent<PhysicsComponent>(pullableUid, out var pullablePhysics))
        {
            return false;
        }

        // Ensure that the puller is not currently pulling anything.
        var oldPullable = pullerComp.Pulling;

        if (oldPullable != null)
        {
            // Well couldn't stop the old one.
            if (!TryStopPull(oldPullable.Value, pullableComp, user))
                return false;
        }

        var pullAttempt = new AttemptPullEvent(pullerPhysics, pullablePhysics);

        RaiseLocalEvent(pullerUid.Owner, pullAttempt, broadcast: false);

        if (pullAttempt.Cancelled)
        {
            return false;
        }

        RaiseLocalEvent(pullableUid.Owner, pullAttempt, true);

        if (pullAttempt.Cancelled)
            return false;

        _interaction.DoContactInteraction(pullableUid.Owner, pullerUid.Owner);

        if (pullableUid != null && pullerUid != null && (pullerUid.Pulling == pullableUid.Owner))
        {
            // Already done
            return;
        }

        // Start by disconnecting the pullable from whatever it is currently connected to.
        var pullableOldPullerE = pullableUid?.Puller;
        if (pullableOldPullerE != null)
        {
            ForceDisconnect(EntityManager.GetComponent<PullerComponent>(pullableOldPullerE.Value), pullableUid!);
        }

        // Continue with the puller.
        var pullerOldPullableE = pullerUid?.Pulling;
        if (pullerOldPullableE != null)
        {
            ForceDisconnect(pullerUid!, EntityManager.GetComponent<PullableComponent>(pullerOldPullableE.Value));
        }

        // And now for the actual connection (if any).

        if (pullerUid != null && pullableUid != null)
        {
            var pullerPhysics = EntityManager.GetComponent<PhysicsComponent>(pullerUid.Owner);
            var pullablePhysics = EntityManager.GetComponent<PhysicsComponent>(pullableUid.Owner);
            pullableUid.PullJointId = $"pull-joint-{pullableUid.Owner}";

            // State startup
            pullerUid.Pulling = pullableUid.Owner;
            pullableUid.Puller = pullerUid.Owner;

            // joint state handling will manage its own state
            if (!_timing.ApplyingState)
            {
                // Joint startup
                var union = _physics.GetHardAABB(pullerUid.Owner).Union(_physics.GetHardAABB(pullableUid.Owner, body: pullablePhysics));
                var length = Math.Max((float) union.Size.X, (float) union.Size.Y) * 0.75f;

                var joint = _jointSystem.CreateDistanceJoint(pullablePhysics.Owner, pullerPhysics.Owner, id: pullableUid.PullJointId);
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
            _alertsSystem.ShowAlert(pullableUid.Owner, AlertType.Pulled);

            RaiseLocalEvent(pullerUid.Owner, message, broadcast: false);
            RaiseLocalEvent(pullableUid.Owner, message, true);

            Dirty(pullerUid);
            Dirty(pullableUid);
        }

        pullableUid.PrevFixedRotation = pullablePhysics.FixedRotation;
        _physics.SetFixedRotation(pullableUid.Owner, pullableUid.FixedRotationOnPull, body: pullablePhysics);
        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(pullerUid.Owner):user} started pulling {ToPrettyString(pullableUid.Owner):target}");
        return true;
    }

    public bool TryStopPull(EntityUid pullableUid, PullableComponent pullable, EntityUid? user = null)
    {
        var pullerUidNull = pullable.Puller;

        if (pullerUidNull == null)
            return false;

        var pullerUid = pullerUidNull.Value;
        var msg = new AttemptStopPullingEvent(user);
        RaiseLocalEvent(pullableUid, msg, true);

        if (msg.Cancelled)
            return false;

        // Stop pulling confirmed!

        if (TryComp<PhysicsComponent>(pullableUid, out var pullablePhysics))
        {
            _physics.SetFixedRotation(pullableUid, pullable.PrevFixedRotation, body: pullablePhysics);
        }

        // Joint shutdown
        if (pullable.PullJointId != null)
        {
            _joints.RemoveJoint(pullableUid, pullable.PullJointId);
            pullable.PullJointId = null;
        }

        pullable.Puller = null;

        if (TryComp<PullerComponent>(pullerUid, out var puller))
        {
            puller.Pulling = null;
            Dirty(pullerUid, puller);
        }

        // Messaging
        var message = new PullStoppedMessage(pullerUid, pullableUid);
        _alertsSystem.ClearAlert(pullerUid, AlertType.Pulling);
        _alertsSystem.ClearAlert(pullableUid, AlertType.Pulled);
        _modifierSystem.RefreshMovementSpeedModifiers(pullerUid);
        _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(pullerUid):user} stopped pulling {ToPrettyString(pullableUid):target}");

        RaiseLocalEvent(pullerUid, message);
        RaiseLocalEvent(pullableUid, message);

        Dirty(pullableUid, pullable);
        return true;
    }
}
