using System.Diagnostics.CodeAnalysis;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Input;
using Content.Shared.Movement.Events;
using Content.Shared.Physics.Pull;
using Content.Shared.Pulling.Components;
using Content.Shared.Pulling.Events;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Players;

namespace Content.Shared.Pulling.Systems;

public sealed class PullingSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly SharedJointSystem _joints = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(SharedPhysicsSystem));
        UpdatesOutsidePrediction = true;

        SubscribeLocalEvent<PullableComponent, MoveInputEvent>(OnPullableMoveInput);
        SubscribeLocalEvent<PullableComponent, JointRemovedEvent>(OnJointRemoved);
        SubscribeLocalEvent<PullableComponent, CollisionChangeEvent>(OnPullableCollisionChange);SubscribeLocalEvent<PullableComponent, PullStartedMessage>(PullableHandlePullStarted);
        SubscribeLocalEvent<PullableComponent, PullStoppedMessage>(PullableHandlePullStopped);

        SubscribeLocalEvent<PullableComponent, GetVerbsEvent<Verb>>(AddPullVerbs);

        SubscribeLocalEvent<PullStartedMessage>(OnPullStarted);
        SubscribeLocalEvent<PullStoppedMessage>(OnPullStopped);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.MovePulledObject, new PointerInputCmdHandler(HandleMovePulledObject))
            .Register<PullingSystem>();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ReleasePulledObject, InputCmdHandler.FromDelegate(HandleReleasePulledObject))
            .Register<PullingSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<PullingSystem>();
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
        if (component.Puller != args.OtherEntity)
            return;

        // Do we have some other join with our Puller?
        // or alternatively:
        // TODO track the relevant joint.

        if (TryComp(uid, out JointComponent? joints))
        {
            foreach (var jt in joints.GetJoints.Values)
            {
                if (jt.BodyAUid == component.Puller || jt.BodyBUid == component.Puller)
                    return;
            }
        }

        // No more joints with puller -> force stop pull.
        _pullSm.ForceDisconnectPullable(component);
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

    // Raise a "you are being pulled" alert if the pulled entity has alerts.
    private void PullableHandlePullStarted(EntityUid uid, PullableComponent component, PullStartedMessage args)
    {
        if (args.Pulled.Owner != uid)
            return;

        _alertsSystem.ShowAlert(uid, AlertType.Pulled);
    }

    private void PullableHandlePullStopped(EntityUid uid, PullableComponent component, PullStoppedMessage args)
    {
        if (args.Pulled.Owner != uid)
            return;

        _alertsSystem.ClearAlert(uid, AlertType.Pulled);
    }

    public bool IsPulled(EntityUid uid, PullableComponent? component = null)
    {
        return Resolve(uid, ref component, false) && component.BeingPulled;
    }

    private void OnPullStarted(PullStartedMessage message)
    {
        SetPuller(message.Puller.Owner, message.Pulled.Owner);
    }

    private void OnPullStopped(PullStoppedMessage message)
    {
        RemovePuller(message.Puller.Owner);
    }

    protected void OnPullableMove(EntityUid uid, PullableComponent component, PullableMoveMessage args)
    {
        _moving.Add(component);
    }

    protected void OnPullableStopMove(EntityUid uid, PullableComponent component, PullableStopMovingMessage args)
    {
        _stoppedMoving.Add(component);
    }

    // TODO: When Joint networking is less shitcodey fix this to use a dedicated joints message.
    private void HandleContainerInsert(EntInsertedIntoContainerMessage message)
    {
        if (TryComp(message.Entity, out PullableComponent? pullable))
        {
            TryStopPull(pullable);
        }

        if (TryComp(message.Entity, out PullerComponent? puller))
        {
            if (puller.Pulling == null) return;

            if (!TryComp(puller.Pulling.Value, out PullableComponent? pulling))
                return;

            TryStopPull(pulling);
        }
    }

    private bool HandleMovePulledObject(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        if (session?.AttachedEntity is not { } player ||
            !player.IsValid())
            return false;

        if (!TryGetPulled(player, out var pulled))
            return false;

        if (!TryComp(pulled.Value, out PullableComponent? pullable))
            return false;

        if (_containerSystem.IsEntityInContainer(player))
            return false;

        TryMoveTo(pullable, coords);

        return false;
    }

    private void SetPuller(EntityUid puller, EntityUid pulled)
    {
        _pullers[puller] = pulled;
    }

    private bool RemovePuller(EntityUid puller)
    {
        return _pullers.Remove(puller);
    }

    public EntityUid GetPulled(EntityUid by)
    {
        return _pullers.GetValueOrDefault(by);
    }

    public bool TryGetPulled(EntityUid by, [NotNullWhen(true)] out EntityUid? pulled)
    {
        return (pulled = GetPulled(by)) != null;
    }

    public bool IsPulling(EntityUid puller, PullerComponent? component = null)
    {
        return Resolve(puller, ref component, false) && component.Pulling != null;
    }

    // A WARNING:
    // The following 2 functions are the most internal part of the pulling system's relationship management.
    // They do not expect to be cancellable.
    private void ForceDisconnect(PullerComponent puller, PullableComponent pullable)
    {
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
                _jointSystem.RemoveJoint(j);
        }
        pullable.PullJointId = null;

        // State shutdown
        puller.Pulling = null;
        pullable.Puller = null;

        // Messaging
        var message = new PullStoppedMessage(pullerPhysics, pullablePhysics);

        RaiseLocalEvent(puller.Owner, message, broadcast: false);

        if (Initialized(pullable.Owner))
            RaiseLocalEvent(pullable.Owner, message, true);

        // Networking
        Dirty(puller);
        Dirty(pullable);
    }

    public void ForceRelationship(PullerComponent? puller, PullableComponent? pullable)
    {
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

            RaiseLocalEvent(puller.Owner, message, broadcast: false);
            RaiseLocalEvent(pullable.Owner, message, true);

            // Networking
            Dirty(puller);
            Dirty(pullable);
        }
    }

    private void HandleReleasePulledObject(ICommonSession? session)
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
}
