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
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Popups;
using Content.Shared.Pulling.Events;
using Content.Shared.Rotation;
using Content.Shared.Standing;
using Content.Shared.Storage.Components;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
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
        args.Handled = TryUnbuckle(ent, ent, ent);
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

    private void OnBuckleInsertIntoEntityStorageAttempt(EntityUid uid, BuckleComponent component, ref InsertIntoEntityStorageAttemptEvent args)
    {
        if (component.Buckled)
            args.Cancelled = true;
    }

    private void OnBucklePreventCollide(EntityUid uid, BuckleComponent component, ref PreventCollideEvent args)
    {
        if (args.OtherEntity == component.BuckledTo && component.DontCollide)
            args.Cancelled = true;
    }

    private void OnBuckleDownAttempt(EntityUid uid, BuckleComponent component, DownAttemptEvent args)
    {
        if (component.Buckled)
            args.Cancel();
    }

    private void OnBuckleStandAttempt(EntityUid uid, BuckleComponent component, StandAttemptEvent args)
    {
        if (component.Buckled)
            args.Cancel();
    }

    private void OnBuckleThrowPushbackAttempt(EntityUid uid, BuckleComponent component, ThrowPushbackAttemptEvent args)
    {
        if (component.Buckled)
            args.Cancel();
    }

    private void OnBuckleUpdateCanMove(EntityUid uid, BuckleComponent component, UpdateCanMoveEvent args)
    {
        if (component.Buckled)
            args.Cancel();
    }

    public bool IsBuckled(EntityUid uid, BuckleComponent? component = null)
    {
        return Resolve(uid, ref component, false) && component.Buckled;
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
    /// <param name="buckleUid"> Uid of the owner of BuckleComponent </param>
    /// <param name="user">
    ///     Uid of a third party entity,
    ///     i.e, the uid of someone else you are dragging to a chair.
    ///     Can equal buckleUid sometimes
    /// </param>
    /// <param name="strapUid"> Uid of the owner of strap component </param>
    /// <param name="strapComp"></param>
    /// <param name="buckleComp"></param>
    private bool CanBuckle(EntityUid buckleUid,
        EntityUid? user,
        EntityUid strapUid,
        bool popup,
        [NotNullWhen(true)] out StrapComponent? strapComp,
        BuckleComponent buckleComp)
    {
        strapComp = null;
        if (!Resolve(strapUid, ref strapComp, false))
            return false;

        // Does it pass the Whitelist
        if (_whitelistSystem.IsWhitelistFail(strapComp.Whitelist, buckleUid) ||
            _whitelistSystem.IsBlacklistPass(strapComp.Blacklist, buckleUid))
        {
            if (popup)
                _popup.PopupClient(Loc.GetString("buckle-component-cannot-fit-message"), user, PopupType.Medium);

            return false;
        }

        if (!_interaction.InRangeUnobstructed(buckleUid,
                strapUid,
                buckleComp.Range,
                predicate: entity => entity == buckleUid || entity == user || entity == strapUid,
                popup: true))
        {
            return false;
        }

        if (!_container.IsInSameOrNoContainer((buckleUid, null, null), (strapUid, null, null)))
            return false;

        if (user != null && !HasComp<HandsComponent>(user))
        {
            if (popup)
                _popup.PopupClient(Loc.GetString("buckle-component-no-hands-message"), user);

            return false;
        }

        if (buckleComp.Buckled && !TryUnbuckle(buckleUid, user, buckleComp))
        {
            if (popup)
            {
                var message = Loc.GetString(buckleUid == user
                    ? "buckle-component-already-buckled-message"
                    : "buckle-component-other-already-buckled-message",
                ("owner", Identity.Entity(buckleUid, EntityManager)));

                _popup.PopupClient(message, user);
            }

            return false;
        }

        // Check whether someone is attempting to buckle something to their own child
        var parent = Transform(strapUid).ParentUid;
        while (parent.IsValid())
        {
            if (parent != buckleUid)
            {
                parent = Transform(parent).ParentUid;
                continue;
            }

            if (popup)
            {
                var message = Loc.GetString(buckleUid == user
                    ? "buckle-component-cannot-buckle-message"
                    : "buckle-component-other-cannot-buckle-message",
                ("owner", Identity.Entity(buckleUid, EntityManager)));

                _popup.PopupClient(message, user);
            }

            return false;
        }

        if (!StrapHasSpace(strapUid, buckleComp, strapComp))
        {
            if (popup)
            {
                var message = Loc.GetString(buckleUid == user
                    ? "buckle-component-cannot-buckle-message"
                    : "buckle-component-other-cannot-buckle-message",
                ("owner", Identity.Entity(buckleUid, EntityManager)));

                _popup.PopupClient(message, user);
            }

            return false;
        }

        var buckleAttempt = new BuckleAttemptEvent((strapUid, strapComp), (buckleUid, buckleComp), user, popup);
        RaiseLocalEvent(buckleUid, ref buckleAttempt);
        if (buckleAttempt.Cancelled)
            return false;

        var strapAttempt = new StrapAttemptEvent((strapUid, strapComp), (buckleUid, buckleComp), user, popup);
        RaiseLocalEvent(strapUid, ref strapAttempt);
        if (strapAttempt.Cancelled)
            return false;

        return true;
    }

    /// <summary>
    /// Attempts to buckle an entity to a strap
    /// </summary>
    /// <param name="buckle"> Uid of the owner of BuckleComponent </param>
    /// <param name="user">
    /// Uid of a third party entity,
    /// i.e, the uid of someone else you are dragging to a chair.
    /// Can equal buckleUid sometimes
    /// </param>
    /// <param name="strap"> Uid of the owner of strap component </param>
    public bool TryBuckle(EntityUid buckle, EntityUid? user, EntityUid strap, BuckleComponent? buckleComp = null, bool popup = true)
    {
        if (!Resolve(buckle, ref buckleComp, false))
            return false;

        if (!CanBuckle(buckle, user, strap, popup, out var strapComp, buckleComp))
            return false;

        Buckle((buckle, buckleComp), (strap, strapComp), user);
        return true;
    }

    private void Buckle(Entity<BuckleComponent> buckle, Entity<StrapComponent> strap, EntityUid? user)
    {
        if (user == buckle.Owner)
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user):player} buckled themselves to {ToPrettyString(strap)}");
        else if (user != null)
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(user):player} buckled {ToPrettyString(buckle)} to {ToPrettyString(strap)}");

        _audio.PlayPredicted(strap.Comp.BuckleSound, strap, user);

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
    /// <param name="buckleUid">The entity to unbuckle.</param>
    /// <param name="user">The entity doing the unbuckling.</param>
    /// <param name="buckleComp">The buckle component of the entity to unbuckle.</param>
    /// <returns>
    ///     true if the owner was unbuckled, otherwise false even if the owner
    ///     was previously already unbuckled.
    /// </returns>
    public bool TryUnbuckle(EntityUid buckleUid,
        EntityUid? user,
        BuckleComponent? buckleComp = null,
        bool popup = true)
    {
        return TryUnbuckle((buckleUid, buckleComp), user, popup);
    }

    public bool TryUnbuckle(Entity<BuckleComponent?> buckle, EntityUid? user, bool popup)
    {
        if (!Resolve(buckle.Owner, ref buckle.Comp, false))
            return false;

        if (!CanUnbuckle(buckle, user, popup, out var strap))
            return false;

        Unbuckle(buckle!, strap, user);
        return true;
    }

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

        if (buckleXform.ParentUid == strap.Owner && !Terminating(buckleXform.ParentUid))
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

    public bool CanUnbuckle(Entity<BuckleComponent?> buckle, EntityUid user, bool popup)
    {
        return CanUnbuckle(buckle, user, popup, out _);
    }

    private bool CanUnbuckle(Entity<BuckleComponent?> buckle, EntityUid? user, bool popup, out Entity<StrapComponent> strap)
    {
        strap = default;
        if (!Resolve(buckle.Owner, ref buckle.Comp))
            return false;

        if (buckle.Comp.BuckledTo is not { } strapUid)
            return false;

        if (!TryComp(strapUid, out StrapComponent? strapComp))
        {
            Log.Error($"Encountered buckle {ToPrettyString(buckle.Owner)} with invalid strap entity {ToPrettyString(strap)}");
            SetBuckledTo(buckle!, null);
            return false;
        }

        strap = (strapUid, strapComp);
        if (_gameTiming.CurTime < buckle.Comp.BuckleTime + buckle.Comp.Delay)
            return false;

        if (user != null && !_interaction.InRangeUnobstructed(user.Value, strap.Owner, buckle.Comp.Range, popup: popup))
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
    /// <param name="args.Target"> The person being put in the chair/bed</param>
    /// <param name="args.User"> The person putting a person in a chair/bed</param>
    /// <param name="args.Used"> The chair/bed </param>

    private void OnBuckleDoafter(Entity<BuckleComponent> entity, ref BuckleDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null || args.Used == null)
            return;

        args.Handled = TryBuckle(args.Target.Value, args.User, args.Used.Value, popup: false);
    }

    /// <summary>
    /// If the target being buckled to a chair/bed goes crit or is cuffed
    /// Cancel the do-after time and try to buckle the target immediately
    /// </summary>
    /// <param name="args.Target"> The person being put in the chair/bed</param>
    /// <param name="args.User"> The person putting a person in a chair/bed</param>
    /// <param name="args.Used"> The chair/bed </param>
    private void BuckleDoafterEarly(Entity<BuckleComponent> entity, BuckleDoAfterEvent args, CancellableEntityEventArgs ev)
    {
        if (args.Target == null || args.Used == null)
            return;

        if (TryComp<CuffableComponent>(args.Target, out var targetCuffableComp) && targetCuffableComp.CuffedHandCount > 0
            || _mobState.IsIncapacitated(args.Target.Value))
        {
            ev.Cancel();
            TryBuckle(args.Target.Value, args.User, args.Used.Value, popup: false);
        }
    }
}
