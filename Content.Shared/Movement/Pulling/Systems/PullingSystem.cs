using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Buckle.Components;
using Content.Shared.Cuffs.Components;
using Content.Shared.Database;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Pulling.Events;
using Content.Shared.Standing;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

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
    [Dependency] private readonly HeldSpeedModifierSystem _clothingMoveSpeed = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtual = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(SharedPhysicsSystem));
        UpdatesOutsidePrediction = true;

        SubscribeLocalEvent<PullableComponent, MoveInputEvent>(OnPullableMoveInput);
        SubscribeLocalEvent<PullableComponent, CollisionChangeEvent>(OnPullableCollisionChange);
        SubscribeLocalEvent<PullableComponent, JointRemovedEvent>(OnJointRemoved);
        SubscribeLocalEvent<PullableComponent, GetVerbsEvent<Verb>>(AddPullVerbs);
        SubscribeLocalEvent<PullableComponent, EntGotInsertedIntoContainerMessage>(OnPullableContainerInsert);
        SubscribeLocalEvent<PullableComponent, ModifyUncuffDurationEvent>(OnModifyUncuffDuration);
        SubscribeLocalEvent<PullableComponent, StopBeingPulledAlertEvent>(OnStopBeingPulledAlert);

        SubscribeLocalEvent<PullerComponent, UpdateMobStateEvent>(OnStateChanged, after: [typeof(MobThresholdSystem)]);
        SubscribeLocalEvent<PullerComponent, AfterAutoHandleStateEvent>(OnAfterState);
        SubscribeLocalEvent<PullerComponent, EntGotInsertedIntoContainerMessage>(OnPullerContainerInsert);
        SubscribeLocalEvent<PullerComponent, EntityUnpausedEvent>(OnPullerUnpaused);
        SubscribeLocalEvent<PullerComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        SubscribeLocalEvent<PullerComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        SubscribeLocalEvent<PullerComponent, DropHandItemsEvent>(OnDropHandItems);
        SubscribeLocalEvent<PullerComponent, StopPullingAlertEvent>(OnStopPullingAlert);

        SubscribeLocalEvent<HandsComponent, PullStartedMessage>(HandlePullStarted);
        SubscribeLocalEvent<HandsComponent, PullStoppedMessage>(HandlePullStopped);

        SubscribeLocalEvent<PullableComponent, StrappedEvent>(OnBuckled);
        SubscribeLocalEvent<PullableComponent, BuckledEvent>(OnGotBuckled);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ReleasePulledObject, InputCmdHandler.FromDelegate(OnReleasePulledObject, handle: false))
            .Register<PullingSystem>();
    }

    private void HandlePullStarted(Entity<HandsComponent> hands, ref PullStartedMessage args)
    {
        if (args.PullerUid != hands.Owner)
            return;

        if (TryComp(args.PullerUid, out PullerComponent? pullerComp) && !pullerComp.NeedsHands)
            return;

        if (!_virtual.TrySpawnVirtualItemInHand(args.PulledUid, hands))
        {
            DebugTools.Assert("Unable to find available hand when starting pulling??");
        }
    }

    private void HandlePullStopped(Entity<HandsComponent> hands, ref PullStoppedMessage args)
    {
        if (args.PullerUid != hands.Owner)
            return;

        // Try find hand that is doing this pull.
        // and clear it.
        foreach (var hand in hands.Comp.Hands.Values)
        {
            if (hand.HeldEntity == null
                || !TryComp(hand.HeldEntity, out VirtualItemComponent? virtualItem)
                || virtualItem.BlockingEntity != args.PulledUid)
            {
                continue;
            }

            _handsSystem.TryDrop(args.PullerUid, hand, handsComp: hands.Comp);
            break;
        }
    }

    private void OnStateChanged(Entity<PullerComponent> puller, ref UpdateMobStateEvent args)
    {
        if (puller.Comp.Pulling == null)
            return;

        if (args.State == MobState.Critical || args.State == MobState.Dead)
        {
            TryStopPull(puller.Comp.Pulling.Value, puller);
        }
    }

    private void OnBuckled(Entity<PullableComponent> ent, ref StrappedEvent args)
    {
        // Prevent people from pulling the entity they are buckled to
        if (ent.Comp.Puller == args.Buckle && !args.Buckle.Comp.PullStrap)
            StopPull(ent.AsNullable());
    }

    private void OnGotBuckled(Entity<PullableComponent> ent, ref BuckledEvent args)
    {
        StopPull(ent.AsNullable());
    }

    private void OnAfterState(Entity<PullerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Comp.Pulling == null)
            RemComp<ActivePullerComponent>(ent);
        else
            EnsureComp<ActivePullerComponent>(ent);
    }

    private void OnDropHandItems(Entity<PullerComponent> puller, ref DropHandItemsEvent args)
    {
        if (puller.Comp.Pulling == null || puller.Comp.NeedsHands)
            return;

        TryStopPull(puller.Comp.Pulling.Value, puller);
    }

    private void OnStopPullingAlert(Entity<PullerComponent> ent, ref StopPullingAlertEvent args)
    {
        if (args.Handled || ent.Comp.Pulling == null)
            return;

        args.Handled = TryStopPull(ent.Comp.Pulling.Value, ent);
    }

    private void OnPullerContainerInsert(Entity<PullerComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (ent.Comp.Pulling == null)
            return;

        TryStopPull(ent.Comp.Pulling.Value, ent.Owner);
    }

    private void OnPullableContainerInsert(Entity<PullableComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        TryStopPull(ent.AsNullable());
    }

    private void OnModifyUncuffDuration(Entity<PullableComponent> ent, ref ModifyUncuffDurationEvent args)
    {
        if (!ent.Comp.BeingPulled)
            return;

        // We don't care if the person is being uncuffed by someone else
        if (args.User != args.Target)
            return;

        args.Duration *= 2;
    }

    private void OnStopBeingPulledAlert(Entity<PullableComponent> ent, ref StopBeingPulledAlertEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryStopPull(ent.AsNullable(), ent);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<PullingSystem>();
    }

    private void OnPullerUnpaused(Entity<PullerComponent> puller, ref EntityUnpausedEvent args)
    {
        puller.Comp.NextThrow += args.PausedTime;
    }

    private void OnVirtualItemDeleted(Entity<PullerComponent> puller, ref VirtualItemDeletedEvent args)
    {
        // If client deletes the virtual hand then stop the pull.
        if (puller.Comp.Pulling == null)
            return;

        if (puller.Comp.Pulling != args.BlockingEntity)
            return;

        TryStopPull(args.BlockingEntity);
    }

    private void AddPullVerbs(Entity<PullableComponent> pullable, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // Are they trying to pull themselves up by their bootstraps?
        if (args.User == args.Target)
            return;

        var puller = args.User;
        var targetPullable = args.Target;

        //TODO VERB ICONS add pulling icon
        if (pullable.Comp.Puller == args.User)
        {
            Verb verb = new()
            {
                Text = Loc.GetString("pulling-verb-get-data-text-stop-pulling"),
                Act = () => TryStopPull(pullable.AsNullable(), puller), // Use the local 'user' copy
                DoContactInteraction = false // pulling handle its own contact interaction.
            };
            args.Verbs.Add(verb);
        }
        else if (CanPull(puller, targetPullable))
        {
            Verb verb = new()
            {
                Text = Loc.GetString("pulling-verb-get-data-text"),
                Act = () => TryStartPull(puller, targetPullable),
                DoContactInteraction = false // pulling handle its own contact interaction.
            };
            args.Verbs.Add(verb);
        }
    }

    private void OnRefreshMovespeed(Entity<PullerComponent> puller, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (TryComp<HeldSpeedModifierComponent>(puller.Comp.Pulling, out var heldMoveSpeed) && puller.Comp.Pulling.HasValue)
        {
            var (walkMod, sprintMod) =
                _clothingMoveSpeed.GetHeldMovementSpeedModifiers(puller.Comp.Pulling.Value, heldMoveSpeed);
            args.ModifySpeed(walkMod, sprintMod);
            return;
        }

        args.ModifySpeed(puller.Comp.WalkSpeedModifier, puller.Comp.SprintSpeedModifier);
    }

    private void OnPullableMoveInput(Entity<PullableComponent> pullable, ref MoveInputEvent args)
    {
        // If someone moves then break their pulling.
        if (!pullable.Comp.BeingPulled)
            return;

        var entity = args.Entity;

        if (!_blocker.CanMove(entity))
            return;

        TryStopPull(pullable.AsNullable(), pullable);
    }

    private void OnPullableCollisionChange(Entity<PullableComponent> pullable, ref CollisionChangeEvent args)
    {
        // IDK what this is supposed to be.
        if (!_timing.ApplyingState && pullable.Comp.PullJointId != null && !args.CanCollide)
        {
            _joints.RemoveJoint(pullable, pullable.Comp.PullJointId);
        }
    }

    private void OnJointRemoved(Entity<PullableComponent> pullable, ref JointRemovedEvent args)
    {
        // Just handles the joint getting nuked without going through pulling system (valid behavior).

        // Not relevant / pullable state handle it.
        if (pullable.Comp.Puller != args.OtherEntity ||
            args.Joint.ID != pullable.Comp.PullJointId ||
            _timing.ApplyingState)
        {
            return;
        }

        if (args.Joint.ID != pullable.Comp.PullJointId || pullable.Comp.Puller == null)
            return;

        StopPull(pullable.AsNullable());
    }

    /// <summary>
    /// Forces pulling to stop and handles cleanup.
    /// </summary>
    /// <param name="pullable">The entity being pulled.</param>
    public void StopPull(Entity<PullableComponent?> pullable)
    {
        if (!Resolve(pullable, ref pullable.Comp, false))
            return;

        if (pullable.Comp.Puller == null)
            return;

        if (!_timing.ApplyingState)
        {
            // Joint shutdown
            if (pullable.Comp.PullJointId != null)
            {
                _joints.RemoveJoint(pullable, pullable.Comp.PullJointId);
                pullable.Comp.PullJointId = null;
            }

            if (TryComp<PhysicsComponent>(pullable, out var pullablePhysics))
            {
                _physics.SetFixedRotation(pullable, pullable.Comp.PrevFixedRotation, body: pullablePhysics);
            }
        }

        var oldPullerUid = pullable.Comp.Puller;
        if (oldPullerUid != null)
            RemComp<ActivePullerComponent>(oldPullerUid.Value);

        pullable.Comp.PullJointId = null;
        pullable.Comp.Puller = null;
        Dirty(pullable);

        // No more joints with puller -> force stop pull.
        if (TryComp<PullerComponent>(oldPullerUid, out var oldPullerComp))
        {
            Entity<PullerComponent> puller = (oldPullerUid.Value, oldPullerComp!);
            _alertsSystem.ClearAlert(puller, puller.Comp.PullingAlert);
            puller.Comp.Pulling = null;
            Dirty(puller);

            // Messaging
            var message = new PullStoppedMessage(puller, pullable);
            _modifierSystem.RefreshMovementSpeedModifiers(puller);
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(puller):puller} stopped pulling {ToPrettyString(pullable):target}");

            RaiseLocalEvent(puller, message);
            RaiseLocalEvent(pullable, message);
        }

        _alertsSystem.ClearAlert(pullable, pullable.Comp.PulledAlert);
    }

    public bool IsPulled(Entity<PullableComponent?> pullable)
    {
        return Resolve(pullable, ref pullable.Comp, false) && pullable.Comp.BeingPulled;
    }

    public bool IsPulling(Entity<PullerComponent?> puller)
    {
        return Resolve(puller, ref puller.Comp, false) && puller.Comp.Pulling != null;
    }

    public EntityUid? GetPuller(Entity<PullableComponent?> pullable)
    {
        return !Resolve(pullable, ref pullable.Comp, false) ? null : pullable.Comp.Puller;
    }

    public EntityUid? GetPulling(Entity<PullerComponent?> puller)
    {
        return !Resolve(puller, ref puller.Comp, false) ? null : puller.Comp.Pulling;
    }

    private void OnReleasePulledObject(ICommonSession? session)
    {
        if (session?.AttachedEntity is not { Valid: true } player)
        {
            return;
        }

        if (!TryComp(player, out PullerComponent? pullerComp) || pullerComp.Pulling == null)
        {
            return;
        }

        TryStopPull(pullerComp.Pulling.Value, player);
    }

    /// <summary>
    /// Checks if puller can pull pullable entity
    /// </summary>
    /// <param name="puller">The entity being pulled.</param>
    /// <param name="pullableUid">The entity doing the pull.</param>
    ///  <returns>
    ///     true if the puller can pull
    /// </returns>
    public bool CanPull(Entity<PullerComponent?> puller, EntityUid pullableUid)
    {
        if (!Resolve(puller, ref puller.Comp, false))
        {
            return false;
        }

        if (puller.Comp.NeedsHands
            && !_handsSystem.TryGetEmptyHand(puller, out _)
            && puller.Comp.Pulling == null)
        {
            return false;
        }

        if (!_blocker.CanInteract(puller, pullableUid))
        {
            return false;
        }

        if (!EntityManager.TryGetComponent<PhysicsComponent>(pullableUid, out var physics))
        {
            return false;
        }

        if (physics.BodyType == BodyType.Static)
        {
            return false;
        }

        if (puller.Owner == pullableUid)
        {
            return false;
        }

        if (!_containerSystem.IsInSameOrNoContainer((puller, null, null), (pullableUid, null, null)))
        {
            return false;
        }

        var getPulled = new BeingPulledAttemptEvent(puller, pullableUid);
        RaiseLocalEvent(pullableUid, getPulled, true);
        var startPull = new StartPullAttemptEvent(puller, pullableUid);
        RaiseLocalEvent(puller, startPull, true);
        return !startPull.Cancelled && !getPulled.Cancelled;
    }

    /// <summary>
    /// Toggle pull action.
    /// If puller does not pull pullable. It starts to pull.
    /// If puller pulls the pullable entity. It stops the pull.
    /// </summary>
    /// <param name="puller">The entity doing the pull.</param>
    /// <param name="nullablePullable">The entity being pulled.</param>
    ///  <returns>
    ///     true if pulling status was succesfully changed.
    /// </returns>
    public bool TogglePull(Entity<PullerComponent?> puller, Entity<PullableComponent?>? nullablePullable = null)
    {
        if (!Resolve(puller, ref puller.Comp, false))
            return false;

        Entity<PullableComponent?> pullable;
        if (nullablePullable == null)
        {
            if (puller.Comp.Pulling != null)
                pullable = (puller.Comp.Pulling.Value, null);
            else
                return false; // puller does not pull, and there is no one to pull. Do nothing.
        }
        else
            pullable = nullablePullable.Value;

        if (!Resolve(pullable, ref pullable.Comp, false))
            return false;

        if (pullable.Comp.Puller == puller)
        {
            return TryStopPull(pullable);
        }

        return TryStartPull(puller, pullable);
    }

    /// <summary>
    /// Try to start pulling action.
    /// Checks if puller can start the action
    /// </summary>
    /// <param name="puller">The entity doing the pull.</param>
    /// <param name="pullable">The entity being pulled.</param>
    ///  <returns>
    ///     true if pulling action was started.
    /// </returns>
    public bool TryStartPull(Entity<PullerComponent?> puller, Entity<PullableComponent?> pullable)
    {
        if (!Resolve(puller, ref puller.Comp, false) ||
            !Resolve(pullable, ref pullable.Comp, false))
        {
            return false;
        }

        if (puller.Comp.Pulling == pullable)
            return true;

        if (!CanPull(puller, pullable))
            return false;

        if (!TryComp(puller, out PhysicsComponent? pullerPhysics) || !TryComp(pullable, out PhysicsComponent? pullablePhysics))
            return false;

        // Ensure that the puller is not currently pulling anything.
        if (puller.Comp.Pulling != null && TryStopPull(puller.Comp.Pulling.Value, puller))
            return false;

        // Stop anyone else pulling the entity we want to pull
        if (pullable.Comp.Puller != null)
        {
            // We're already pulling this item
            if (pullable.Comp.Puller == puller)
                return false;

            if (!TryStopPull(pullable, puller))
                return false;
        }

        var pullAttempt = new PullAttemptEvent(puller, pullable);
        RaiseLocalEvent(puller, pullAttempt);

        if (pullAttempt.Cancelled)
            return false;

        RaiseLocalEvent(pullable, pullAttempt);

        if (pullAttempt.Cancelled)
            return false;

        // Pulling confirmed

        _interaction.DoContactInteraction(pullable, puller);

        // Use net entity so it's consistent across client and server.
        pullable.Comp.PullJointId = $"pull-joint-{GetNetEntity(pullable)}";

        EnsureComp<ActivePullerComponent>(puller);
        puller.Comp.Pulling = pullable;
        pullable.Comp.Puller = puller;

        // store the pulled entity's physics FixedRotation setting in case we change it
        pullable.Comp.PrevFixedRotation = pullablePhysics.FixedRotation;

        // joint state handling will manage its own state
        if (!_timing.ApplyingState)
        {
            var joint = _joints.CreateDistanceJoint(pullable, puller,
                    pullablePhysics.LocalCenter, pullerPhysics.LocalCenter,
                    id: pullable.Comp.PullJointId);
            joint.CollideConnected = false;
            // This maximum has to be there because if the object is constrained too closely, the clamping goes backwards and asserts.
            // Internally, the joint length has been set to the distance between the pivots.
            // Add an additional 15cm (pretty arbitrary) to the maximum length for the hard limit.
            joint.MaxLength = joint.Length + 0.15f;
            joint.MinLength = 0f;
            // Set the spring stiffness to zero. The joint won't have any effect provided
            // the current length is beteen MinLength and MaxLength. At those limits, the
            // joint will have infinite stiffness.
            joint.Stiffness = 0f;

            _physics.SetFixedRotation(pullable, pullable.Comp.FixedRotationOnPull, body: pullablePhysics);
        }

        // Messaging
        var message = new PullStartedMessage(puller, pullable);
        _modifierSystem.RefreshMovementSpeedModifiers(puller);
        _alertsSystem.ShowAlert(puller, puller.Comp.PullingAlert);
        _alertsSystem.ShowAlert(pullable, pullable.Comp.PulledAlert);

        RaiseLocalEvent(puller, message);
        RaiseLocalEvent(pullable, message);

        Dirty(puller);
        Dirty(pullable);

        var pullingMessage =
            Loc.GetString("getting-pulled-popup", ("puller", Identity.Entity(puller, EntityManager)));
        _popup.PopupEntity(pullingMessage, pullable, pullable);

        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(puller):puller} started pulling {ToPrettyString(pullable):target}");
        return true;
    }

    [Obsolete("Use Entity<T> variant")]
    /// <summary>
    /// Try to start pulling action.
    /// Checks if puller can start the action
    /// </summary>
    /// <param name="puller">The entity doing the pull.</param>
    /// <param name="pullable">The entity being pulled.</param>
    ///  <returns>
    ///     true if pulling action was started.
    /// </returns>
    public bool TryStopPull(EntityUid pullableUid, PullableComponent pullableComp, EntityUid? puller = null)
    {
        return TryStopPull((pullableUid, pullableComp), puller);
    }

    /// <summary>
    /// Attempts to stop pulling action made by puller
    /// </summary>
    /// <param name="pullable">The entity being pulled.</param>
    /// <param name="puller">The entity doing the pull.</param>
    ///  <returns>
    ///     true if the puller stoped the pull
    /// </returns>
    public bool TryStopPull(Entity<PullableComponent?> pullable, EntityUid? puller = null)
    {
        if (!Resolve(pullable, ref pullable.Comp, false))
            return false;

        if (!CanStopPull(pullable, puller))
            return false;

        StopPull(pullable);
        return true;
    }

    /// <summary>
    /// Checks if User can stop pulling action
    /// </summary>
    /// <param name="pullable">The entity being pulled.</param>
    /// <param name="puller">The entity doing the pull.</param>
    ///  <returns>
    ///     true if the puller can stop the pull
    /// </returns>
    public bool CanStopPull(Entity<PullableComponent?> pullable, EntityUid? puller = null)
    {
        if (!Resolve(pullable.Owner, ref pullable.Comp, false))
            return false;

        if (pullable.Comp.Puller == null)
            return true;

        if (puller != null && !_blocker.CanInteract(puller.Value, pullable.Owner))
            return false;

        var msg = new AttemptStopPullingEvent(puller);
        RaiseLocalEvent(pullable.Owner, msg, true);

        if (msg.Cancelled)
            return false;
        return true;
    }
}
