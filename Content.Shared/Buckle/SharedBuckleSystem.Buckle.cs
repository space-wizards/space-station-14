using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.Alert;
using Content.Shared.Buckle.Components;
using Content.Shared.Cuffs.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Pulling.Events;
using Content.Shared.Standing;
using Content.Shared.Storage.Components;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Buckle;

public abstract partial class SharedBuckleSystem
{
    public static ProtoId<AlertCategoryPrototype> BuckledAlertCategory = "Buckled";

    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly PullingSystem _pullingSystem = default!;

    private void InitializeBuckle()
    {
        SubscribeLocalEvent<BuckleComponent, ComponentShutdown>(OnBuckleComponentShutdown);
        SubscribeLocalEvent<BuckleComponent, MoveEvent>(OnBuckleMove);
        SubscribeLocalEvent<BuckleComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<BuckleComponent, EntGotInsertedIntoContainerMessage>(OnInserted);

        SubscribeLocalEvent<BuckleComponent, StartPullAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<BuckleComponent, BeingPulledAttemptEvent>(OnBeingPulledAttempt);
        SubscribeLocalEvent<BuckleComponent, PullStartedMessage>(OnPullStarted);
        SubscribeLocalEvent<BuckleComponent, UnbuckleAlertEvent>(OnUnbuckleAlert);

        SubscribeLocalEvent<BuckleComponent, InsertIntoEntityStorageAttemptEvent>(OnBuckleInsertIntoEntityStorageAttempt);

        SubscribeLocalEvent<BuckleComponent, PreventCollideEvent>(OnBucklePreventCollide);
        SubscribeLocalEvent<BuckleComponent, DownAttemptEvent>(OnBuckleDownAttempt);
        SubscribeLocalEvent<BuckleComponent, StandAttemptEvent>(OnBuckleStandAttempt);
        SubscribeLocalEvent<BuckleComponent, ThrowPushbackAttemptEvent>(OnBuckleThrowPushbackAttempt);
        SubscribeLocalEvent<BuckleComponent, UpdateCanMoveEvent>(OnBuckleUpdateCanMove);

        SubscribeLocalEvent<BuckleComponent, BuckleDoAfterEvent>(OnBuckleDoafter);
        SubscribeLocalEvent<BuckleComponent, DoAfterAttemptEvent<BuckleDoAfterEvent>>((uid, comp, ev) =>
        {
            BuckleDoafterEarly((uid, comp), ev.Event, ev);
        });
    }

    private void OnBuckleComponentShutdown(Entity<BuckleComponent> ent, ref ComponentShutdown args)
    {
        Unbuckle(ent!, null);
    }

    #region Pulling

    private void OnPullAttempt(Entity<BuckleComponent> ent, ref StartPullAttemptEvent args)
    {
        // Prevent people pulling the chair they're on, etc.
        if (ent.Comp.BuckledTo == args.Pulled && !ent.Comp.PullStrap)
            args.Cancel();
    }

    private void OnBeingPulledAttempt(Entity<BuckleComponent> ent, ref BeingPulledAttemptEvent args)
    {
        if (args.Cancelled || !ent.Comp.Buckled)
            return;

        if (!CanUnbuckle(ent!, args.Puller, false))
            args.Cancel();
    }

    private void OnPullStarted(Entity<BuckleComponent> ent, ref PullStartedMessage args)
    {
        Unbuckle(ent!, args.PullerUid);
    }

    private void OnUnbuckleAlert(Entity<BuckleComponent> ent, ref UnbuckleAlertEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = TryUnbuckle(ent, ent);
    }

    #endregion

    #region Transform

    private void OnParentChanged(Entity<BuckleComponent> ent, ref EntParentChangedMessage args)
    {
        BuckleTransformCheck(ent, args.Transform);
    }

    private void OnInserted(Entity<BuckleComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        BuckleTransformCheck(ent, Transform(ent));
    }

    private void OnBuckleMove(Entity<BuckleComponent> ent, ref MoveEvent ev)
    {
        BuckleTransformCheck(ent, ev.Component);
    }

    /// <summary>
    /// Check if the entity should get unbuckled as a result of transform or container changes.
    /// </summary>
    private void BuckleTransformCheck(Entity<BuckleComponent> buckle, TransformComponent xform)
    {
        if (_gameTiming.ApplyingState)
            return;

        if (buckle.Comp.BuckledTo is not { } strapUid)
            return;

        if (!TryComp<StrapComponent>(strapUid, out var strapComp))
        {
            Log.Error($"Encountered buckle entity {ToPrettyString(buckle)} without a valid strap entity {ToPrettyString(strapUid)}");
            SetBuckledTo(buckle, null);
            return;
        }

        if (xform.ParentUid != strapUid || _container.IsEntityInContainer(buckle))
        {
            Unbuckle(buckle, (strapUid, strapComp), null);
            return;
        }

        var delta = (xform.LocalPosition - strapComp.BuckleOffset).LengthSquared();
        if (delta > 1e-5)
            Unbuckle(buckle, (strapUid, strapComp), null);
    }

    #endregion

    private void OnBuckleInsertIntoEntityStorageAttempt(Entity<BuckleComponent> buckle, ref InsertIntoEntityStorageAttemptEvent args)
    {
        if (buckle.Comp.Buckled)
            args.Cancelled = true;
    }

    private void OnBucklePreventCollide(Entity<BuckleComponent> buckle, ref PreventCollideEvent args)
    {
        if (args.OtherEntity == buckle.Comp.BuckledTo && buckle.Comp.DontCollide)
            args.Cancelled = true;
    }

    private void OnBuckleDownAttempt(Entity<BuckleComponent> buckle, ref DownAttemptEvent args)
    {
        if (buckle.Comp.Buckled)
            args.Cancel();
    }

    private void OnBuckleStandAttempt(Entity<BuckleComponent> buckle, ref StandAttemptEvent args)
    {
        if (buckle.Comp.Buckled)
            args.Cancel();
    }

    private void OnBuckleThrowPushbackAttempt(Entity<BuckleComponent> buckle, ref ThrowPushbackAttemptEvent args)
    {
        if (buckle.Comp.Buckled)
            args.Cancel();
    }

    private void OnBuckleUpdateCanMove(Entity<BuckleComponent> buckle, ref UpdateCanMoveEvent args)
    {
        if (buckle.Comp.Buckled)
            args.Cancel();
    }

    public bool IsBuckled(Entity<BuckleComponent?> buckle)
    {
        return Resolve(buckle.Owner, ref buckle.Comp, false) && buckle.Comp.Buckled;
    }

    protected void SetBuckledTo(Entity<BuckleComponent> buckle, Entity<StrapComponent?>? strap)
    {
        if (TryComp(buckle.Comp.BuckledTo, out StrapComponent? old))
        {
            old.BuckledEntities.Remove(buckle);
            Dirty(buckle.Comp.BuckledTo.Value, old);
        }

        if (strap is {} strapEnt && Resolve(strapEnt.Owner, ref strapEnt.Comp))
        {
            strapEnt.Comp.BuckledEntities.Add(buckle);
            Dirty(strapEnt);
            _alerts.ShowAlert(buckle, strapEnt.Comp.BuckledAlertType);
        }
        else
        {
            _alerts.ClearAlertCategory(buckle, BuckledAlertCategory);
        }

        buckle.Comp.BuckledTo = strap;
        buckle.Comp.BuckleTime = _gameTiming.CurTime;
        ActionBlocker.UpdateCanMove(buckle);
        Appearance.SetData(buckle, StrapVisuals.State, buckle.Comp.Buckled);
        Dirty(buckle);
    }

    /// <summary>
    /// Checks whether or not buckling is possible
    /// </summary>
    /// <param name="userUid">
    ///     Uid of a third party entity,
    ///     i.e, the uid of someone else you are dragging to a chair.
    ///     Can equal buckleUid sometimes
    /// </param>
    /// <returns>
    ///     true if userUid can perform buckling
    /// </returns> 
    private bool CanBuckle(Entity<BuckleComponent> buckle, Entity<StrapComponent> strap, EntityUid userUid, bool popup)
    {
        // Does it pass the Whitelist
        if (_whitelistSystem.IsWhitelistFail(strap.Comp.Whitelist, buckle.Owner) ||
            _whitelistSystem.IsBlacklistPass(strap.Comp.Blacklist, buckle.Owner))
        {
            if (popup)
                _popup.PopupClient(Loc.GetString("buckle-component-cannot-fit-message"), userUid, PopupType.Medium);

            return false;
        }

        if (!_interaction.InRangeUnobstructed(buckle.Owner,
                strap.Owner,
                buckle.Comp.Range,
                predicate: entity => entity == buckle.Owner || entity == userUid || entity == strap.Owner,
                popup: true))
        {
            return false;
        }

        if (!_container.IsInSameOrNoContainer((buckle.Owner, null, null), (strap.Owner, null, null)))
            return false;

        if (!HasComp<HandsComponent>(userUid))
        {
            if (popup)
                _popup.PopupClient(Loc.GetString("buckle-component-no-hands-message"), userUid);

            return false;
        }

        if (buckle.Comp.Buckled && !CanUnbuckle(buckle.Owner, userUid, true))
        {
            if (popup)
            {
                var message = Loc.GetString(buckle.Owner == userUid
                    ? "buckle-component-already-buckled-message"
                    : "buckle-component-other-already-buckled-message",
                ("owner", Identity.Entity(buckle.Owner, EntityManager)));

                _popup.PopupClient(message, userUid);
            }

            return false;
        }

        if (TryComp<PullableComponent>(strap.Owner, out var pullable) && pullable.Puller == buckle.Owner)
        {
            if (!_pullingSystem.CanStopPull((strap.Owner, pullable)))
                return false;
        }

        // Check whether someone is attempting to buckle something to their own child
        var parent = Transform(strap.Owner).ParentUid;
        while (parent.IsValid())
        {
            if (parent != buckle.Owner)
            {
                parent = Transform(parent).ParentUid;
                continue;
            }

            if (popup)
            {
                var message = Loc.GetString(buckle.Owner == userUid
                    ? "buckle-component-cannot-buckle-message"
                    : "buckle-component-other-cannot-buckle-message",
                ("owner", Identity.Entity(buckle.Owner, EntityManager)));

                _popup.PopupClient(message, userUid);
            }

            return false;
        }

        if (!StrapHasSpace(strap.Owner, buckle.Comp, strap.Comp))
        {
            if (popup)
            {
                var message = Loc.GetString(buckle.Owner == userUid
                    ? "buckle-component-cannot-buckle-message"
                    : "buckle-component-other-cannot-buckle-message",
                ("owner", Identity.Entity(buckle.Owner, EntityManager)));

                _popup.PopupClient(message, userUid);
            }

            return false;
        }

        var buckleAttempt = new BuckleAttemptEvent(strap, buckle, userUid, popup);
        RaiseLocalEvent(buckle.Owner, ref buckleAttempt);
        if (buckleAttempt.Cancelled)
            return false;

        var strapAttempt = new StrapAttemptEvent(strap, buckle, userUid, popup);
        RaiseLocalEvent(strap.Owner, ref strapAttempt);
        if (strapAttempt.Cancelled)
            return false;

        return true;
    }

    /// <summary>
    /// Attempts to buckle an entity to a strap
    /// </summary>
    /// <param name="buckle">The entity to buckle.</param>
    /// <param name="userUid">The entity doing the buckling.</param>
    /// <param name="popup">Should there be popup.</param>
    ///  <returns>
    ///     true if the owner was buckled
    /// </returns>
    public bool TryBuckle(Entity<BuckleComponent?> buckle, EntityUid? userUid, Entity<StrapComponent?> strap, bool popup = true)
    {
        if (!Resolve(buckle.Owner, ref buckle.Comp, false))
            return false;

        if (!Resolve(strap.Owner, ref strap.Comp, false))
            return false;

        if (userUid is null)
            return false;

        if (!CanBuckle(buckle!, strap!, userUid.Value, popup))
            return false;

        Buckle(buckle!, strap!, userUid);
        return true;
    }

    private void Buckle(Entity<BuckleComponent> buckle, Entity<StrapComponent> strap, EntityUid? userUid)
    {
        if (userUid == buckle.Owner)
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(userUid):player} buckled themselves to {ToPrettyString(strap)}");
        else if (userUid != null)
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(userUid):player} buckled {ToPrettyString(buckle)} to {ToPrettyString(strap)}");

        if (TryComp<PullableComponent>(strap.Owner, out var pullable) && pullable.Puller == buckle.Owner)
        {
            _pullingSystem.StopPull((strap.Owner, pullable));
        }

        if (buckle.Comp.Buckled)
        {
            Unbuckle(buckle.Owner, userUid);
        }

        _audio.PlayPredicted(strap.Comp.BuckleSound, strap, userUid);

        SetBuckledTo(buckle, strap!);
        Appearance.SetData(strap, StrapVisuals.State, true);
        Appearance.SetData(buckle, BuckleVisuals.Buckled, true);

        _rotationVisuals.SetHorizontalAngle(buckle.Owner, strap.Comp.Rotation);

        var xform = Transform(buckle);
        var coords = new EntityCoordinates(strap, strap.Comp.BuckleOffset);
        _transform.SetCoordinates(buckle, xform, coords, rotation: Angle.Zero);

        _joints.SetRelay(buckle, strap);

        switch (strap.Comp.Position)
        {
            case StrapPosition.Stand:
                _standing.Stand(buckle, force: true);
                break;
            case StrapPosition.Down:
                _standing.Down(buckle, false, false, force: true);
                break;
        }

        var ev = new StrappedEvent(strap, buckle);
        RaiseLocalEvent(strap, ref ev);

        var gotEv = new BuckledEvent(strap, buckle);
        RaiseLocalEvent(buckle, ref gotEv);

        if (TryComp<PhysicsComponent>(buckle, out var physics))
            _physics.ResetDynamics(buckle, physics);

        DebugTools.AssertEqual(xform.ParentUid, strap.Owner);
    }

    /// <summary>
    /// Tries to unbuckle the Owner of this component from its current strap.
    /// </summary>
    /// <param name="buckle">The entity to unbuckle.</param>
    /// <param name="userUid">The entity doing the unbuckling.</param>
    /// <param name="popup">Should there be popup.</param>
    /// <returns>
    ///     true if the owner was unbuckled, otherwise false even if the owner
    ///     was previously already unbuckled.
    /// </returns>
    public bool TryUnbuckle(Entity<BuckleComponent?> buckle, EntityUid? userUid, bool popup = true)
    {
        if (!Resolve(buckle.Owner, ref buckle.Comp, false))
            return false;

        if (userUid is null)
            return false;

        return TryUnbuckle(buckle!, userUid!, popup);
    }

    /// <summary>
    /// Tries to unbuckle the Owner of this component from its current strap.
    /// </summary>
    /// <param name="buckle">The entity to unbuckle.</param>
    /// <param name="user">The entity doing the unbuckling.</param>
    /// <param name="popup">Should there be popup.</param>
    /// <returns>
    ///     true if the owner was unbuckled, otherwise false even if the owner
    ///     was previously already unbuckled.
    /// </returns>
    private bool TryUnbuckle(Entity<BuckleComponent> buckle, EntityUid user, bool popup = true)
    {
        Entity<StrapComponent> strap;

        if (!GetStrapfromBuckle(buckle, out strap))
            return false;

        if (!CanUnbuckle(buckle, strap, user, popup))
            return false;

        Unbuckle(buckle!, strap, user);
        return true;
    }

    private bool GetStrapfromBuckle(Entity<BuckleComponent> buckle, out Entity<StrapComponent> strap)
    {
        strap = default;

        if (buckle.Comp.BuckledTo is not { } strapUid)
            return false;

        if (!TryComp(strapUid, out StrapComponent? strapComp))
        {
            Log.Error($"Encountered buckle {ToPrettyString(buckle.Owner)} with invalid strap entity {ToPrettyString(strap)}");
            SetBuckledTo(buckle!, null);
            return false;
        }
        strap = (strapUid, strapComp);
        return true;
    }

    /// <summary>
    /// Force unbuckle performed by user.
    /// Use Try TryUnbuckle if you want check is user actually can unbuckle 
    /// </summary>
    /// <param name="buckle">The entity to unbuckle.</param>
    /// <param name="user">The entity doing the unbuckling.</param>
    /// <summary>
    public void Unbuckle(Entity<BuckleComponent?> buckle, EntityUid? user)
    {
        if (!Resolve(buckle.Owner, ref buckle.Comp, false))
            return;

        if (buckle.Comp.BuckledTo is not { } strap)
            return;

        if (!TryComp(strap, out StrapComponent? strapComp))
        {
            Log.Error($"Encountered buckle {ToPrettyString(buckle.Owner)} with invalid strap entity {ToPrettyString(strap)}");
            SetBuckledTo(buckle!, null);
            return;
        }

        Unbuckle(buckle!, (strap, strapComp), user);
    }

    /// <summary>
    /// Force unbuckle performed by user.
    /// Use Try TryUnbuckle if you want check is user actually can unbuckle 
    /// </summary>
    /// <param name="buckle">The entity to unbuckle.</param>
    /// <param name="user">The entity doing the unbuckling.</param>
    /// <summary>
    private void Unbuckle(Entity<BuckleComponent> buckle, Entity<StrapComponent> strap, EntityUid? user)
    {
        if (user == buckle.Owner)
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{user} unbuckled themselves from {strap}");
        else if (user != null)
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{user} unbuckled {buckle} from {strap}");

        _audio.PlayPredicted(strap.Comp.UnbuckleSound, strap, user);

        SetBuckledTo(buckle, null);

        var buckleXform = Transform(buckle);
        var oldBuckledXform = Transform(strap);

        if (buckleXform.ParentUid == strap.Owner && !Terminating(oldBuckledXform.ParentUid))
        {
            _transform.PlaceNextTo((buckle, buckleXform), (strap.Owner, oldBuckledXform));
            buckleXform.ActivelyLerping = false;

            var oldBuckledToWorldRot = _transform.GetWorldRotation(strap);
            _transform.SetWorldRotationNoLerp((buckle, buckleXform), oldBuckledToWorldRot);

            // TODO: This is doing 4 moveevents this is why I left the warning in, if you're going to remove it make it only do 1 moveevent.
            if (strap.Comp.BuckleOffset != Vector2.Zero)
            {
                buckleXform.Coordinates = oldBuckledXform.Coordinates.Offset(strap.Comp.BuckleOffset);
            }
        }

        _rotationVisuals.ResetHorizontalAngle(buckle.Owner);
        Appearance.SetData(strap, StrapVisuals.State, strap.Comp.BuckledEntities.Count != 0);
        Appearance.SetData(buckle, BuckleVisuals.Buckled, false);

        if (HasComp<KnockedDownComponent>(buckle) || _mobState.IsIncapacitated(buckle))
            _standing.Down(buckle, playSound: false);
        else
            _standing.Stand(buckle);

        _joints.RefreshRelay(buckle);

        var buckleEv = new UnbuckledEvent(strap, buckle);
        RaiseLocalEvent(buckle, ref buckleEv);

        var strapEv = new UnstrappedEvent(strap, buckle);
        RaiseLocalEvent(strap, ref strapEv);
    }

    /// <summary>
    /// Can user perform unbuckle.
    /// </summary>
    /// <param name="buckle">buckled entity to unbuckle.</param>
    /// <param name="user">The entity doing the unbuckling.</param>
    /// <param name="popup">Should there be popup.</param>
    /// <returns>
    ///     true if user can unbuckled the buckled entity
    /// </returns>
    public bool CanUnbuckle(Entity<BuckleComponent?> buckle, EntityUid? user, bool popup)
    {
        if (user is null)
            return false;

        if (!Resolve(buckle.Owner, ref buckle.Comp, false))
            return false;

        if (!GetStrapfromBuckle(buckle!, out var strap))
            return false;

        return CanUnbuckle(buckle!, strap!, user.Value, popup);
    }

    /// <summary>
    /// Can user perform unbuckle.
    /// </summary>
    /// <param name="buckle">buckled entity to unbuckle.</param>
    /// <param name="user">The entity doing the unbuckling.</param>
    /// <param name="popup">Should there be popup.</param>
    /// <returns>
    ///     true if user can unbuckled the buckled entity
    /// </returns>
    private bool CanUnbuckle(Entity<BuckleComponent> buckle, Entity<StrapComponent> strap, EntityUid user, bool popup)
    {
        if (_gameTiming.CurTime < buckle.Comp.BuckleTime + buckle.Comp.Delay)
            return false;

        if (!_interaction.InRangeUnobstructed(user, strap.Owner, buckle.Comp.Range, popup: popup))
            return false;

        var unbuckleAttempt = new UnbuckleAttemptEvent(strap, buckle!, user, popup);
        RaiseLocalEvent(buckle, ref unbuckleAttempt);
        if (unbuckleAttempt.Cancelled)
            return false;

        var unstrapAttempt = new UnstrapAttemptEvent(strap, buckle!, user, popup);
        RaiseLocalEvent(strap, ref unstrapAttempt);
        return !unstrapAttempt.Cancelled;
    }

    /// <summary>
    /// Once the do-after is complete, try to buckle target to chair/bed
    /// </summary>
    /// <param name="buckle"> Entity being buckled to strap</param>
    /// <param name="args.User"> The person buckling a person to a strap</param>
    /// <param name="args.Used"> strap entity </param>
    private void OnBuckleDoafter(Entity<BuckleComponent> buckle, ref BuckleDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null || args.Used == null)
            return;

        args.Handled = TryBuckle(buckle.AsNullable(), args.User, args.Used.Value, popup: false);
    }

    /// <summary>
    /// If the target being buckled to a chair/bed goes crit or is cuffed
    /// Cancel the do-after time and try to buckle the target immediately
    /// </summary>
    /// <param name="buckle"> The person being put in the chair/bed</param>
    /// <param name="args.User"> The person putting a person in a chair/bed</param>
    /// <param name="args.Used"> The chair/bed </param>
    private void BuckleDoafterEarly(Entity<BuckleComponent> buckle, BuckleDoAfterEvent args, CancellableEntityEventArgs ev)
    {
        if (args.Used == null)
            return;

        if (TryComp<CuffableComponent>(buckle, out var targetCuffableComp) && targetCuffableComp.CuffedHandCount > 0
            || _mobState.IsIncapacitated(buckle))
        {
            ev.Cancel();
            TryBuckle(buckle.AsNullable(), args.User, args.Used.Value, popup: false);
        }
    }
}
