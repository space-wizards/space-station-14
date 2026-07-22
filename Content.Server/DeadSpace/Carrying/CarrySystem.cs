// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Numerics;
using Content.Server.Popups;
using Content.Server.Stunnable;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Buckle.Components;
using Content.Shared.Database;
using Content.Shared.DeadSpace.Carrying;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.RepulseAttract.Events;
using Content.Shared.Standing;
using Content.Shared.IdentityManagement;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Server.DeadSpace.Carrying;

public sealed class CarrySystem : EntitySystem
{
    private const int RequiredHands = 2;
    private const float CarriedThrowMaxDistance = 3.5f;
    private static readonly TimeSpan HumanoidThrowKnockdownTime = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan HeavyBodyPickupTime = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan LightBodyPickupTime = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan AnimalPickupTime = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan EscapeDuration = TimeSpan.FromSeconds(7);
    private static readonly TimeSpan EscapePopupInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan EscapeKnockdownTime = TimeSpan.FromSeconds(1.5f);

    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtual = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CarrySizeComponent, GetVerbsEvent<AlternativeVerb>>(OnGetCarryVerb);
        SubscribeLocalEvent<CarrySizeComponent, CarryDoAfterEvent>(OnCarryDoAfter);
        SubscribeLocalEvent<CarryingComponent, BeforeThrowEvent>(OnBeforeThrow);
        SubscribeLocalEvent<CarryingComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        SubscribeLocalEvent<CarryingComponent, EntGotInsertedIntoContainerMessage>(OnCarrierInsertedIntoContainer);
        SubscribeLocalEvent<CarryingComponent, BuckleAttemptEvent>(OnCarrierBuckleAttempt);
        SubscribeLocalEvent<CarryingComponent, BuckledEvent>(OnCarrierBuckled);
        SubscribeLocalEvent<CarryingComponent, DownedEvent>(OnCarrierDowned);
        SubscribeLocalEvent<CarryingComponent, MobStateChangedEvent>(OnCarrierMobStateChanged);
        SubscribeLocalEvent<CarryingComponent, EntityTerminatingEvent>(OnCarrierTerminating);
        SubscribeLocalEvent<CarryingComponent, ComponentShutdown>(OnCarryingShutdown);
        SubscribeLocalEvent<CarriedComponent, EntGotInsertedIntoContainerMessage>(OnCarriedInsertedIntoContainer);
        SubscribeLocalEvent<CarriedComponent, BuckledEvent>(OnCarriedBuckled);
        SubscribeLocalEvent<CarriedComponent, EntParentChangedMessage>(OnCarriedParentChanged);
        SubscribeLocalEvent<CarriedComponent, BeforeRepulseAttractThrownEvent>(OnCarriedRepulseAttractThrown);
        SubscribeLocalEvent<CarriedComponent, MoveInputEvent>(OnCarriedMoveInput);
        SubscribeLocalEvent<CarriedComponent, AttackAttemptEvent>(OnCarriedAttackAttempt);
        SubscribeLocalEvent<CarriedComponent, StandAttemptEvent>(OnCarriedStandAttempt);
        SubscribeLocalEvent<CarriedComponent, MobStateChangedEvent>(OnCarriedMobStateChanged);
        SubscribeLocalEvent<CarriedComponent, EntityTerminatingEvent>(OnCarriedTerminating);
        SubscribeLocalEvent<CarriedComponent, ComponentShutdown>(OnCarriedShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<CarriedComponent>();
        while (query.MoveNext(out var uid, out var carried))
        {
            if (carried.Carrier is not { } carrier ||
                !TryComp<CarryingComponent>(carrier, out var carrying) ||
                carrying.Carried != uid)
            {
                CleanupInvalidCarriedState(uid, carried);
                continue;
            }

            if (!carried.EscapeInProgress)
                continue;

            if (TryComp<MobStateComponent>(uid, out var mobState) && _mobState.IsIncapacitated(uid, mobState))
            {
                StopCarryEscape(uid, carried);
                continue;
            }

            if (time >= carried.EscapeCompleteTime)
            {
                CompleteCarryEscape(uid, carried, carrier, carrying);
                continue;
            }

            if (time < carried.NextEscapePopupTime)
                continue;

            PopupCarryEscapeAttempt(uid, carrier);
            carried.NextEscapePopupTime = time + EscapePopupInterval;
        }
    }

    private void OnGetCarryVerb(Entity<CarrySizeComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (!CanCarry(args.User, ent.Owner, out _))
            return;

        var user = args.User;
        var target = ent.Owner;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("carry-verb-pick-up"),
            Act = () => TryStartCarry(user, target),
            Priority = 10,
            DoContactInteraction = false,
            Impact = LogImpact.Medium,
        });
    }

    private void OnCarryDoAfter(Entity<CarrySizeComponent> ent, ref CarryDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        TryCarryNow(args.User, ent.Owner);
    }

    public bool TryStartCarry(EntityUid carrier, EntityUid target, bool popup = true)
    {
        if (!CanCarryWithPullingFixup(carrier, target, popup))
            return false;

        var delay = GetPickupDelay(carrier, target);
        var doAfter = new DoAfterArgs(EntityManager, carrier, delay, new CarryDoAfterEvent(), target, target: target)
        {
            BlockDuplicate = true,
            BreakOnDamage = true,
            BreakOnMove = true,
            DistanceThreshold = 1.5f,
        };
        var started = _doAfter.TryStartDoAfter(doAfter);
        if (started)
        {
            _popup.PopupEntity(Loc.GetString("carry-popup-being-picked-up", ("user", Identity.Entity(carrier, EntityManager))), target, target);
        }
        return started;
    }

    public bool CanCarry(EntityUid carrier, EntityUid target)
    {
        return CanCarry(carrier, target, out _);
    }

    public bool TryStopCarry(EntityUid carrier)
    {
        if (!TryComp<CarryingComponent>(carrier, out var carrying))
            return false;

        StopCarry(carrier, carrying, keepTargetDown: true);
        return true;
    }

    private bool TryCarryNow(EntityUid carrier, EntityUid target, bool popup = true)
    {
        if (!CanCarryWithPullingFixup(carrier, target, popup))
            return false;

        if (!_hands.TryGetEmptyHand(carrier, out var firstHand) ||
            !_virtual.TrySpawnVirtualItemInHand(target, carrier, out var firstVirtual, empty: firstHand))
        {
            if (popup)
                _popup.PopupEntity(Loc.GetString("carry-popup-no-free-hands"), carrier, carrier);

            return false;
        }

        if (!_hands.TryGetEmptyHand(carrier, out var secondHand) ||
            !_virtual.TrySpawnVirtualItemInHand(target, carrier, out var secondVirtual, empty: secondHand))
        {
            _virtual.DeleteInHandsMatching(carrier, target);

            if (popup)
                _popup.PopupEntity(Loc.GetString("carry-popup-no-free-hands"), carrier, carrier);

            return false;
        }

        var carrying = EnsureComp<CarryingComponent>(carrier);
        carrying.Carried = target;
        carrying.VirtualItems.Clear();
        carrying.VirtualItems.Add(firstVirtual.Value);
        carrying.VirtualItems.Add(secondVirtual.Value);
        Dirty(carrier, carrying);

        var carried = EnsureComp<CarriedComponent>(target);
        carried.Carrier = carrier;
        carried.Stopping = false;
        carried.AddedBlockMovement = !HasComp<BlockMovementComponent>(target);
        carried.PreviousCanCollide = null;
        carried.ForcedDown = false;
        carried.WasStanding = false;
        carried.EscapeInProgress = false;
        carried.EscapeCompleteTime = TimeSpan.Zero;
        carried.NextEscapePopupTime = TimeSpan.Zero;

        if (carried.AddedBlockMovement)
            EnsureComp<BlockMovementComponent>(target);

        ForceHumanoidDown(target, carried);

        if (TryComp<PhysicsComponent>(target, out var physics))
        {
            carried.PreviousCanCollide = physics.CanCollide;
            _physics.SetCanCollide(target, false, body: physics);
            _physics.ResetDynamics(target, physics);
        }

        Dirty(target, carried);

        var targetXform = Transform(target);
        _transform.SetCoordinates(target, targetXform, new EntityCoordinates(carrier, Vector2.Zero), rotation: Angle.Zero);
        _interaction.DoContactInteraction(target, carrier);

        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(carrier):user} started carrying {ToPrettyString(target):target}");
        return true;
    }

    private bool CanCarryWithPullingFixup(EntityUid carrier, EntityUid target, bool popup)
    {
        if (!CanCarry(carrier, target, out var failure))
        {
            if (failure == "carry-popup-no-free-hands")
            {
                StopPullingConflicts(carrier, target);

                if (!CanCarry(carrier, target, out failure))
                {
                    if (popup && failure != null)
                        _popup.PopupEntity(Loc.GetString(failure), carrier, carrier);

                    return false;
                }
            }
            else
            {
                if (popup && failure != null)
                    _popup.PopupEntity(Loc.GetString(failure), carrier, carrier);

                return false;
            }
        }

        StopPullingConflicts(carrier, target);
        return true;
    }

    private TimeSpan GetPickupDelay(EntityUid carrier, EntityUid target)
    {
        if (TryComp<InstantCriticalCarryComponent>(carrier, out var instant) &&
            TryComp<MobStateComponent>(target, out var mob) &&
            instant.States.Contains(mob.CurrentState))
        {
            return TimeSpan.Zero;
        }

        if (HasComp<MobStateComponent>(target) && !HasComp<HumanoidAppearanceComponent>(target))
            return AnimalPickupTime;

        return TryComp<CarrySizeComponent>(target, out var size) && size.Size == CarrySize.Small
            ? LightBodyPickupTime
            : HeavyBodyPickupTime;
    }

    private bool CanCarry(EntityUid carrier, EntityUid target, out string? failure)
    {
        failure = null;

        if (carrier == target)
        {
            failure = "carry-popup-self";
            return false;
        }

        if (Deleted(carrier) || Deleted(target))
        {
            failure = "carry-popup-invalid-target";
            return false;
        }

        if (TryComp<StandingStateComponent>(carrier, out var carrierStanding) && !carrierStanding.Standing)
        {
            failure = "carry-popup-not-standing";
            return false;
        }

        if (TryComp<BuckleComponent>(carrier, out var carrierBuckle) && carrierBuckle.Buckled)
        {
            failure = "carry-popup-carrier-buckled";
            return false;
        }

        if (HasComp<CarryingComponent>(carrier) || HasComp<CarriedComponent>(carrier))
        {
            failure = "carry-popup-busy";
            return false;
        }

        if (HasComp<CarriedComponent>(target))
        {
            failure = "carry-popup-target-busy";
            return false;
        }

        if (HasComp<CarryingComponent>(target))
        {
            failure = "carry-popup-target-carrying";
            return false;
        }

        if (!TryComp<CarryStrengthComponent>(carrier, out var strength))
        {
            failure = "carry-popup-no-strength";
            return false;
        }

        if (!TryComp<CarrySizeComponent>(target, out var size))
        {
            failure = "carry-popup-not-carryable";
            return false;
        }

        if (strength.Strength == CarryStrength.SmallOnly && size.Size != CarrySize.Small)
        {
            failure = "carry-popup-too-large";
            return false;
        }

        if (!_actionBlocker.CanInteract(carrier, target))
        {
            failure = "carry-popup-cannot-interact";
            return false;
        }

        if (!_container.IsInSameOrNoContainer(carrier, target) || _container.IsEntityInContainer(target))
        {
            failure = "carry-popup-invalid-target";
            return false;
        }

        if (TryComp<BuckleComponent>(target, out var buckle) && buckle.Buckled)
        {
            failure = "carry-popup-target-buckled";
            return false;
        }

        var xform = Transform(target);
        if (xform.Anchored || !TryComp<PhysicsComponent>(target, out var physics) || physics.BodyType == BodyType.Static)
        {
            failure = "carry-popup-invalid-target";
            return false;
        }

        if (!TryComp<HandsComponent>(carrier, out var hands) || _hands.GetEmptyHandCount((carrier, hands)) < RequiredHands)
        {
            failure = "carry-popup-no-free-hands";
            return false;
        }

        return true;
    }

    private void OnBeforeThrow(Entity<CarryingComponent> ent, ref BeforeThrowEvent args)
    {
        if (args.Cancelled)
            return;

        if (ent.Comp.Carried is not { } target)
            return;

        if (!TryComp<VirtualItemComponent>(args.ItemUid, out var virtualItem) || virtualItem.BlockingEntity != target)
            return;

        args.Cancelled = true;

        var direction = args.Direction;
        if (direction.LengthSquared() > CarriedThrowMaxDistance * CarriedThrowMaxDistance)
            direction = Vector2.Normalize(direction) * CarriedThrowMaxDistance;

        if (HasComp<HumanoidAppearanceComponent>(target))
            _stun.TryUpdateParalyzeDuration(target, HumanoidThrowKnockdownTime);

        StopCarry(ent.Owner, ent.Comp, thrown: true);

        if (direction.LengthSquared() <= 0.001f)
            return;

        _throwing.TryThrow(
            target,
            direction,
            args.ThrowSpeed,
            ent.Owner,
            pushbackRatio: 0f,
            compensateFriction: true,
            recoil: false,
            doSpin: false);
    }

    private void OnVirtualItemDeleted(Entity<CarryingComponent> ent, ref VirtualItemDeletedEvent args)
    {
        if (ent.Comp.Stopping || ent.Comp.Carried != args.BlockingEntity)
            return;

        StopCarry(
            ent.Owner,
            ent.Comp,
            placeTarget: !TerminatingOrDeleted(ent.Owner),
            keepTargetDown: true,
            inheritCarrierVelocity: true);
    }

    private void OnCarrierInsertedIntoContainer(Entity<CarryingComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        StopCarry(ent.Owner, ent.Comp, placeTarget: false, keepTargetDown: true);
    }

    private void OnCarrierBuckleAttempt(Entity<CarryingComponent> ent, ref BuckleAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        args.Cancelled = true;

        if (args.Popup && args.User is { } user)
            _popup.PopupEntity(Loc.GetString("carry-popup-carrier-carrying"), ent.Owner, user);
    }

    private void OnCarrierBuckled(Entity<CarryingComponent> ent, ref BuckledEvent args)
    {
        StopCarry(ent.Owner, ent.Comp, keepTargetDown: true);
    }

    private void OnCarrierDowned(Entity<CarryingComponent> ent, ref DownedEvent args)
    {
        StopCarry(ent.Owner, ent.Comp, keepTargetDown: true);
    }

    private void OnCarrierMobStateChanged(Entity<CarryingComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState is MobState.Alive)
            return;

        StopCarry(ent.Owner, ent.Comp, keepTargetDown: true);
    }

    private void OnCarrierTerminating(Entity<CarryingComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.Stopping)
            return;

        CleanupCarry(ent.Owner, ent.Comp, thrown: false, placeTarget: false, removeCarrierComponent: false);
    }

    private void OnCarriedInsertedIntoContainer(Entity<CarriedComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (ent.Comp.Carrier is not { } carrier || !TryComp<CarryingComponent>(carrier, out var carrying))
            return;

        StopCarry(carrier, carrying, placeTarget: false);
    }

    private void OnCarriedBuckled(Entity<CarriedComponent> ent, ref BuckledEvent args)
    {
        if (ent.Comp.Carrier is not { } carrier || !TryComp<CarryingComponent>(carrier, out var carrying))
            return;

        StopCarry(carrier, carrying, placeTarget: false, keepTargetDown: true);
    }

    private void OnCarriedParentChanged(Entity<CarriedComponent> ent, ref EntParentChangedMessage args)
    {
        if (_timing.ApplyingState || ent.Comp.Stopping)
            return;

        if (ent.Comp.Carrier is not { } carrier || args.Transform.ParentUid == carrier)
            return;

        if (!TryComp<CarryingComponent>(carrier, out var carrying) || carrying.Carried != ent.Owner)
        {
            CleanupInvalidCarriedState(ent.Owner, ent.Comp);
            return;
        }

        StopCarry(carrier, carrying, placeTarget: false, keepTargetDown: true);
    }

    private void OnCarriedRepulseAttractThrown(Entity<CarriedComponent> ent, ref BeforeRepulseAttractThrownEvent args)
    {
        if (ent.Comp.Carrier is not { } carrier ||
            !TryComp<CarryingComponent>(carrier, out var carrying) ||
            carrying.Carried != ent.Owner)
        {
            CleanupInvalidCarriedState(ent.Owner, ent.Comp);
            return;
        }

        StopCarry(carrier, carrying, placeTarget: false, keepTargetDown: true);
    }

    private void OnCarriedMoveInput(Entity<CarriedComponent> ent, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        TryStartCarryEscape(ent);
    }

    private void OnCarriedAttackAttempt(Entity<CarriedComponent> ent, ref AttackAttemptEvent args)
    {
        if (ent.Comp.Carrier == null || args.Target != ent.Comp.Carrier)
            return;

        if (TryStartCarryEscape(ent))
            args.Cancel();
    }

    private void OnCarriedStandAttempt(Entity<CarriedComponent> ent, ref StandAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnCarriedMobStateChanged(Entity<CarriedComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.OldMobState == MobState.Alive || args.NewMobState != MobState.Alive)
            return;

        if (ent.Comp.Carrier is not { } carrier || !TryComp<CarryingComponent>(carrier, out var carrying))
            return;

        StopCarry(carrier, carrying, keepTargetDown: false);
    }

    private void OnCarriedTerminating(Entity<CarriedComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.Stopping || ent.Comp.Carrier is not { } carrier || !TryComp<CarryingComponent>(carrier, out var carrying))
            return;

        CleanupCarry(carrier, carrying, thrown: false, placeTarget: false, removeCarriedComponent: false);
    }

    private void OnCarryingShutdown(Entity<CarryingComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Stopping)
            return;

        CleanupCarry(ent.Owner, ent.Comp, thrown: false, placeTarget: !TerminatingOrDeleted(ent.Owner), removeCarrierComponent: false);
    }

    private void OnCarriedShutdown(Entity<CarriedComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.Stopping || ent.Comp.Carrier is not { } carrier || !TryComp<CarryingComponent>(carrier, out var carrying))
            return;

        CleanupCarry(carrier, carrying, thrown: false, placeTarget: false, removeCarriedComponent: false);
    }

    private void StopCarry(
        EntityUid carrier,
        CarryingComponent carrying,
        bool thrown = false,
        bool placeTarget = true,
        bool keepTargetDown = true,
        bool inheritCarrierVelocity = false)
    {
        placeTarget &= !TerminatingOrDeleted(carrier);

        CleanupCarry(
            carrier,
            carrying,
            thrown,
            placeTarget,
            keepTargetDown: keepTargetDown,
            inheritCarrierVelocity: inheritCarrierVelocity);
    }

    private void CleanupCarry(
        EntityUid carrier,
        CarryingComponent carrying,
        bool thrown,
        bool placeTarget,
        bool removeCarrierComponent = true,
        bool removeCarriedComponent = true,
        bool keepTargetDown = true,
        bool inheritCarrierVelocity = false)
    {
        if (carrying.Stopping)
            return;

        carrying.Stopping = true;
        var target = carrying.Carried;

        CarriedComponent? carried = null;
        if (target != null)
            TryComp(target.Value, out carried);

        if (carried != null)
            carried.Stopping = true;

        if (target is { } targetUid && !Deleted(targetUid))
        {
            RestoreCarried(targetUid, carried, carrier, placeTarget, thrown, keepTargetDown, inheritCarrierVelocity);

            if (removeCarriedComponent && HasComp<CarriedComponent>(targetUid))
                RemComp<CarriedComponent>(targetUid);
        }

        if (!TerminatingOrDeleted(carrier))
            _virtual.DeleteInHandsMatching(carrier, target ?? EntityUid.Invalid);

        carrying.Carried = null;
        carrying.VirtualItems.Clear();

        if (removeCarrierComponent && HasComp<CarryingComponent>(carrier))
            RemComp<CarryingComponent>(carrier);

        if (target is { } logTarget)
            _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(carrier):user} stopped carrying {ToPrettyString(logTarget):target}");
    }

    private void RestoreCarried(
        EntityUid target,
        CarriedComponent? carried,
        EntityUid carrier,
        bool placeTarget,
        bool thrown,
        bool keepTargetDown,
        bool inheritCarrierVelocity)
    {
        var inheritedVelocity = Vector2.Zero;
        if (inheritCarrierVelocity && TryComp<PhysicsComponent>(carrier, out var carrierPhysics))
            inheritedVelocity = carrierPhysics.LinearVelocity;

        if (carried?.AddedBlockMovement == true && HasComp<BlockMovementComponent>(target))
            RemComp<BlockMovementComponent>(target);

        if (TryComp<PhysicsComponent>(target, out var physics))
        {
            if (carried?.PreviousCanCollide is { } previous)
                _physics.SetCanCollide(target, previous, body: physics);

            _physics.ResetDynamics(target, physics);
        }

        RestoreForcedDown(target, carried, thrown, keepTargetDown);
        _actionBlocker.UpdateCanMove(target);

        if (TerminatingOrDeleted(target))
            return;

        if (!placeTarget || TerminatingOrDeleted(carrier))
        {
            var xform = Transform(target);

            if (xform.ParentUid == carrier)
                _transform.AttachToGridOrMap(target);

            return;
        }

        if (thrown)
        {
            var carrierCoordinates = Transform(carrier).Coordinates;
            _transform.SetCoordinates(target, Transform(target), carrierCoordinates, rotation: Angle.Zero);
            _transform.AttachToGridOrMap(target);
        }
        else
        {
            _transform.PlaceNextTo(target, carrier);
        }

        if (!inheritCarrierVelocity || inheritedVelocity.LengthSquared() <= 0.001f)
            return;

        if (TryComp<PhysicsComponent>(target, out var targetPhysics))
            _physics.SetLinearVelocity(target, inheritedVelocity, body: targetPhysics);
    }

    private void ForceHumanoidDown(EntityUid target, CarriedComponent carried)
    {
        if (!HasComp<HumanoidAppearanceComponent>(target) ||
            !TryComp<StandingStateComponent>(target, out var standing) ||
            !standing.Standing)
        {
            return;
        }

        carried.WasStanding = true;

        if (_standing.Down(target, playSound: false, dropHeldItems: false, force: true, standingState: standing))
            carried.ForcedDown = true;
    }

    private void RestoreForcedDown(EntityUid target, CarriedComponent? carried, bool thrown, bool keepTargetDown)
    {
        if (thrown ||
            carried?.ForcedDown != true ||
            !carried.WasStanding ||
            TerminatingOrDeleted(target) ||
            HasComp<KnockedDownComponent>(target) ||
            HasComp<StunnedComponent>(target) ||
            TryComp<BuckleComponent>(target, out var buckle) && buckle.Buckled ||
            TryComp<MobStateComponent>(target, out var mobState) && _mobState.IsIncapacitated(target, mobState))
        {
            return;
        }

        if (keepTargetDown)
        {
            _stun.TryCrawling(target, null, autoStand: false, drop: false, force: true);
            return;
        }

        _standing.Stand(target, force: true);
    }

    private bool TryStartCarryEscape(Entity<CarriedComponent> ent)
    {
        if (ent.Comp.EscapeInProgress)
            return true;

        if (ent.Comp.Carrier is not { } carrier || !HasComp<CarryingComponent>(carrier))
            return false;

        if (TryComp<MobStateComponent>(ent.Owner, out var mobState) && _mobState.IsIncapacitated(ent.Owner, mobState))
            return false;

        var time = _timing.CurTime;
        ent.Comp.EscapeInProgress = true;
        ent.Comp.EscapeCompleteTime = time + EscapeDuration;
        ent.Comp.NextEscapePopupTime = time + EscapePopupInterval;

        PopupCarryEscapeAttempt(ent.Owner, carrier);
        Dirty(ent.Owner, ent.Comp);

        return true;
    }

    private void CompleteCarryEscape(EntityUid target, CarriedComponent carried, EntityUid carrier, CarryingComponent carrying)
    {
        StopCarryEscape(target, carried);
        StopCarry(carrier, carrying, keepTargetDown: true);
        EnsureCarryEscapeCleanup(target, carried, carrier, carrying);

        _stun.TryKnockdown(target, EscapeKnockdownTime, autoStand: false, drop: false, force: true);
        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(target):target} escaped from being carried by {ToPrettyString(carrier):user}");
    }

    private void CleanupInvalidCarriedState(EntityUid target, CarriedComponent carried)
    {
        var carrier = carried.Carrier;

        if (carried.AddedBlockMovement && HasComp<BlockMovementComponent>(target))
            RemComp<BlockMovementComponent>(target);

        if (TryComp<PhysicsComponent>(target, out var physics))
        {
            if (carried.PreviousCanCollide is { } previous)
                _physics.SetCanCollide(target, previous, body: physics);

            _physics.ResetDynamics(target, physics);
        }

        RestoreForcedDown(target, carried, thrown: false, keepTargetDown: true);
        StopCarryEscape(target, carried);

        if (!TerminatingOrDeleted(target))
        {
            var xform = Transform(target);

            if (carrier == null || TerminatingOrDeleted(carrier.Value) || xform.ParentUid == carrier.Value)
                _transform.AttachToGridOrMap(target);
        }

        if (carrier is { } carrierUid && !TerminatingOrDeleted(carrierUid))
            _virtual.DeleteInHandsMatching(carrierUid, target);

        if (HasComp<CarriedComponent>(target))
            RemComp<CarriedComponent>(target);

        _actionBlocker.UpdateCanMove(target);
    }

    private void EnsureCarryEscapeCleanup(EntityUid target, CarriedComponent carried, EntityUid carrier, CarryingComponent carrying)
    {
        if (carried.AddedBlockMovement && HasComp<BlockMovementComponent>(target))
            RemComp<BlockMovementComponent>(target);

        if (HasComp<CarriedComponent>(target))
            RemComp<CarriedComponent>(target);

        if (carrying.Carried == target && HasComp<CarryingComponent>(carrier))
            RemComp<CarryingComponent>(carrier);

        _actionBlocker.UpdateCanMove(target);
    }

    private void StopCarryEscape(EntityUid target, CarriedComponent carried)
    {
        if (!carried.EscapeInProgress)
            return;

        carried.EscapeInProgress = false;
        carried.EscapeCompleteTime = TimeSpan.Zero;
        carried.NextEscapePopupTime = TimeSpan.Zero;
        Dirty(target, carried);
    }

    private void PopupCarryEscapeAttempt(EntityUid target, EntityUid carrier)
    {
        _popup.PopupEntity(Loc.GetString("carry-popup-escape-victim"), target, target);
        _popup.PopupEntity(Loc.GetString("carry-popup-escape-carrier"), carrier, carrier);
    }

    private void StopPullingConflicts(EntityUid carrier, EntityUid target)
    {
        if (TryComp<PullerComponent>(carrier, out var carrierPuller) &&
            carrierPuller.Pulling == target &&
            TryComp<PullableComponent>(target, out var targetPullable))
        {
            _pulling.TryStopPull(target, targetPullable, carrier);
        }

        if (TryComp<PullableComponent>(target, out var pullable) && pullable.Puller != null)
            _pulling.TryStopPull(target, pullable, carrier);

        if (TryComp<PullerComponent>(target, out var targetPuller) &&
            targetPuller.Pulling is { } pulledByTarget &&
            TryComp<PullableComponent>(pulledByTarget, out var pulledByTargetComp))
        {
            _pulling.TryStopPull(pulledByTarget, pulledByTargetComp, target);
        }
    }
}
