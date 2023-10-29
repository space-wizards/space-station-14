using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.Alert;
using Content.Shared.Bed.Sleep;
using Content.Shared.Buckle.Components;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Pulling.Components;
using Content.Shared.Standing;
using Content.Shared.Storage.Components;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Vehicle.Components;
using Content.Shared.Verbs;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Utility;

namespace Content.Shared.Buckle;

public abstract partial class SharedBuckleSystem
{
    private void InitializeBuckle()
    {
        SubscribeLocalEvent<BuckleComponent, ComponentStartup>(OnBuckleComponentStartup);
        SubscribeLocalEvent<BuckleComponent, ComponentShutdown>(OnBuckleComponentShutdown);
        SubscribeLocalEvent<BuckleComponent, MoveEvent>(OnBuckleMove);
        SubscribeLocalEvent<BuckleComponent, InteractHandEvent>(OnBuckleInteractHand);
        SubscribeLocalEvent<BuckleComponent, GetVerbsEvent<InteractionVerb>>(AddUnbuckleVerb);
        SubscribeLocalEvent<BuckleComponent, InsertIntoEntityStorageAttemptEvent>(OnBuckleInsertIntoEntityStorageAttempt);

        SubscribeLocalEvent<BuckleComponent, PreventCollideEvent>(OnBucklePreventCollide);
        SubscribeLocalEvent<BuckleComponent, DownAttemptEvent>(OnBuckleDownAttempt);
        SubscribeLocalEvent<BuckleComponent, StandAttemptEvent>(OnBuckleStandAttempt);
        SubscribeLocalEvent<BuckleComponent, ThrowPushbackAttemptEvent>(OnBuckleThrowPushbackAttempt);
        SubscribeLocalEvent<BuckleComponent, UpdateCanMoveEvent>(OnBuckleUpdateCanMove);
        SubscribeLocalEvent<BuckleComponent, ChangeDirectionAttemptEvent>(OnBuckleChangeDirectionAttempt);
    }

    private void OnBuckleComponentStartup(EntityUid uid, BuckleComponent component, ComponentStartup args)
    {
        UpdateBuckleStatus(uid, component);
    }

    private void OnBuckleComponentShutdown(EntityUid uid, BuckleComponent component, ComponentShutdown args)
    {
        TryUnbuckle(uid, uid, true, component);

        component.BuckleTime = default;
    }

    private void OnBuckleMove(EntityUid uid, BuckleComponent component, ref MoveEvent ev)
    {
        if (component.BuckledTo is not {} strapUid)
            return;

        if (!TryComp<StrapComponent>(strapUid, out var strapComp))
            return;

        var strapPosition = Transform(strapUid).Coordinates;
        if (ev.NewPosition.InRange(EntityManager, _transform, strapPosition, strapComp.MaxBuckleDistance))
            return;

        TryUnbuckle(uid, uid, true, component);
    }

    private void OnBuckleInteractHand(EntityUid uid, BuckleComponent component, InteractHandEvent args)
    {
        if (!component.Buckled)
            return;

        if (TryUnbuckle(uid, args.User, buckleComp: component))
            args.Handled = true;
    }

    private void AddUnbuckleVerb(EntityUid uid, BuckleComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !component.Buckled)
            return;

        InteractionVerb verb = new()
        {
            Act = () => TryUnbuckle(uid, args.User, buckleComp: component),
            Text = Loc.GetString("verb-categories-unbuckle"),
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/unbuckle.svg.192dpi.png"))
        };

        if (args.Target == args.User && args.Using == null)
        {
            // A user is left clicking themselves with an empty hand, while buckled.
            // It is very likely they are trying to unbuckle themselves.
            verb.Priority = 1;
        }

        args.Verbs.Add(verb);
    }

    private void OnBuckleInsertIntoEntityStorageAttempt(EntityUid uid, BuckleComponent component, ref InsertIntoEntityStorageAttemptEvent args)
    {
        if (component.Buckled)
            args.Cancelled = true;
    }

    private void OnBucklePreventCollide(EntityUid uid, BuckleComponent component, ref PreventCollideEvent args)
    {
        if (args.OtherEntity != component.BuckledTo)
            return;

        if (component.Buckled || component.DontCollide)
            args.Cancelled = true;
    }

    private void OnBuckleDownAttempt(EntityUid uid, BuckleComponent component, DownAttemptEvent args)
    {
        if (component.Buckled)
            args.Cancel();
    }

    private void OnBuckleStandAttempt(EntityUid uid, BuckleComponent component, StandAttemptEvent args)
    {
        //Let entities stand back up while on vehicles so that they can be knocked down when slept/stunned
        //This prevents an exploit that allowed people to become partially invulnerable to stuns
        //while on vehicles

        if (component.BuckledTo != null)
        {
            var buckle = component.BuckledTo;
            if (TryComp<VehicleComponent>(buckle, out _))
                return;
        }
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
        if (component.LifeStage > ComponentLifeStage.Running)
            return;

        if (component.Buckled &&
            !HasComp<VehicleComponent>(component.BuckledTo)) // buckle+vehicle shitcode
            args.Cancel();
    }

    private void OnBuckleChangeDirectionAttempt(EntityUid uid, BuckleComponent component, ChangeDirectionAttemptEvent args)
    {
        if (component.Buckled)
            args.Cancel();
    }

    public bool IsBuckled(EntityUid uid, BuckleComponent? component = null)
    {
        return Resolve(uid, ref component, false) && component.Buckled;
    }

    /// <summary>
    /// Shows or hides the buckled status effect depending on if the
    /// entity is buckled or not.
    /// </summary>
    /// <param name="uid"> Entity that we want to show the alert </param>
    /// <param name="buckleComp"> buckle component of the entity </param>
    /// <param name="strapComp"> strap component of the thing we are strapping to </param>
    private void UpdateBuckleStatus(EntityUid uid, BuckleComponent buckleComp, StrapComponent? strapComp = null)
    {
        Appearance.SetData(uid, StrapVisuals.State, buckleComp.Buckled);
        if (buckleComp.BuckledTo != null)
        {
            if (!Resolve(buckleComp.BuckledTo.Value, ref strapComp))
                return;

            var alertType = strapComp.BuckledAlertType;
            _alerts.ShowAlert(uid, alertType);
        }
        else
        {
            _alerts.ClearAlertCategory(uid, AlertCategory.Buckled);
        }
    }

    /// <summary>
    /// Sets the <see cref="BuckleComponent.BuckledTo"/> field in the component to a value
    /// </summary>
    /// <param name="strapUid"> Value tat with be assigned to the field </param>
    private void SetBuckledTo(EntityUid buckleUid, EntityUid? strapUid, StrapComponent? strapComp, BuckleComponent buckleComp)
    {
        buckleComp.BuckledTo = strapUid;

        if (strapUid == null)
        {
            buckleComp.Buckled = false;
        }
        else
        {
            buckleComp.LastEntityBuckledTo = strapUid;
            buckleComp.DontCollide = true;
            buckleComp.Buckled = true;
            buckleComp.BuckleTime = _gameTiming.CurTime;
        }

        ActionBlocker.UpdateCanMove(buckleUid);
        UpdateBuckleStatus(buckleUid, buckleComp, strapComp);
        Dirty(buckleComp);
    }

    /// <summary>
    /// Checks whether or not buckling is possible
    /// </summary>
    /// <param name="buckleUid"> Uid of the owner of BuckleComponent </param>
    /// <param name="userUid">
    /// Uid of a third party entity,
    /// i.e, the uid of someone else you are dragging to a chair.
    /// Can equal buckleUid sometimes
    /// </param>
    /// <param name="strapUid"> Uid of the owner of strap component </param>
    private bool CanBuckle(
        EntityUid buckleUid,
        EntityUid userUid,
        EntityUid strapUid,
        [NotNullWhen(true)] out StrapComponent? strapComp,
        BuckleComponent? buckleComp = null)
    {
        strapComp = null;

        if (userUid == strapUid ||
            !Resolve(buckleUid, ref buckleComp, false) ||
            !Resolve(strapUid, ref strapComp, false))
        {
            return false;
        }

        // Does it pass the Whitelist
        if (strapComp.AllowedEntities != null &&
            !strapComp.AllowedEntities.IsValid(userUid, EntityManager))
        {
            if (_netManager.IsServer)
                _popup.PopupEntity(Loc.GetString("buckle-component-cannot-fit-message"), userUid, buckleUid, PopupType.Medium);
            return false;
        }

        // Is it within range
        bool Ignored(EntityUid entity) => entity == buckleUid || entity == userUid || entity == strapUid;

        if (!_interaction.InRangeUnobstructed(buckleUid, strapUid, buckleComp.Range, predicate: Ignored,
                popup: true))
        {
            return false;
        }

        // If in a container
        if (_container.TryGetContainingContainer(buckleUid, out var ownerContainer))
        {
            // And not in the same container as the strap
            if (!_container.TryGetContainingContainer(strapUid, out var strapContainer) ||
                ownerContainer != strapContainer)
            {
                return false;
            }
        }

        if (!HasComp<HandsComponent>(userUid))
        {
            // PopupPredicted when
            if (_netManager.IsServer)
                _popup.PopupEntity(Loc.GetString("buckle-component-no-hands-message"), userUid, userUid);
            return false;
        }

        if (buckleComp.Buckled)
        {
            var message = Loc.GetString(buckleUid == userUid
                    ? "buckle-component-already-buckled-message"
                    : "buckle-component-other-already-buckled-message",
                ("owner", Identity.Entity(buckleUid, EntityManager)));
            if (_netManager.IsServer)
                _popup.PopupEntity(message, userUid, userUid);

            return false;
        }

        var parent = Transform(strapUid).ParentUid;
        while (parent.IsValid())
        {
            if (parent == userUid)
            {
                var message = Loc.GetString(buckleUid == userUid
                    ? "buckle-component-cannot-buckle-message"
                    : "buckle-component-other-cannot-buckle-message", ("owner", Identity.Entity(buckleUid, EntityManager)));
                if (_netManager.IsServer)
                    _popup.PopupEntity(message, userUid, userUid);

                return false;
            }

            parent = Transform(parent).ParentUid;
        }

        if (!StrapHasSpace(strapUid, buckleComp, strapComp))
        {
            var message = Loc.GetString(buckleUid == userUid
                ? "buckle-component-cannot-fit-message"
                : "buckle-component-other-cannot-fit-message", ("owner", Identity.Entity(buckleUid, EntityManager)));
            if (_netManager.IsServer)
                _popup.PopupEntity(message, userUid, userUid);

            return false;
        }

        var attemptEvent = new BuckleAttemptEvent(strapUid, buckleUid, userUid, true);
        RaiseLocalEvent(attemptEvent.BuckledEntity, ref attemptEvent);
        RaiseLocalEvent(attemptEvent.StrapEntity, ref attemptEvent);
        if (attemptEvent.Cancelled)
            return false;

        return true;
    }

    /// <summary>
    /// Attempts to buckle an entity to a strap
    /// </summary>
    /// <param name="buckleUid"> Uid of the owner of BuckleComponent </param>
    /// <param name="userUid">
    /// Uid of a third party entity,
    /// i.e, the uid of someone else you are dragging to a chair.
    /// Can equal buckleUid sometimes
    /// </param>
    /// <param name="strapUid"> Uid of the owner of strap component </param>
    public bool TryBuckle(EntityUid buckleUid, EntityUid userUid, EntityUid strapUid, BuckleComponent? buckleComp = null)
    {
        if (!Resolve(buckleUid, ref buckleComp, false))
            return false;

        if (!CanBuckle(buckleUid, userUid, strapUid, out var strapComp, buckleComp))
            return false;

        if (!StrapTryAdd(strapUid, buckleUid, buckleComp, false, strapComp))
        {
            var message = Loc.GetString(buckleUid == userUid
                ? "buckle-component-cannot-buckle-message"
                : "buckle-component-other-cannot-buckle-message", ("owner", Identity.Entity(buckleUid, EntityManager)));
            if (_netManager.IsServer)
                _popup.PopupEntity(message, userUid, userUid);
            return false;
        }

        if (TryComp<AppearanceComponent>(buckleUid, out var appearance))
            Appearance.SetData(buckleUid, BuckleVisuals.Buckled, true, appearance);

        ReAttach(buckleUid, strapUid, buckleComp, strapComp);
        SetBuckledTo(buckleUid, strapUid, strapComp, buckleComp);
        // TODO user is currently set to null because if it isn't the sound fails to play in some situations, fix that
        var audioSourceUid = userUid == buckleUid ? userUid : strapUid;
        _audio.PlayPredicted(strapComp.BuckleSound, strapUid, audioSourceUid);

        var ev = new BuckleChangeEvent(strapUid, buckleUid, true);
        RaiseLocalEvent(ev.BuckledEntity, ref ev);
        RaiseLocalEvent(ev.StrapEntity, ref ev);

        if (TryComp<SharedPullableComponent>(buckleUid, out var ownerPullable))
        {
            if (ownerPullable.Puller != null)
            {
                _pulling.TryStopPull(ownerPullable);
            }
        }

        if (TryComp<PhysicsComponent>(buckleUid, out var physics))
        {
            _physics.ResetDynamics(physics);
        }

        if (!buckleComp.PullStrap && TryComp<SharedPullableComponent>(strapUid, out var toPullable))
        {
            if (toPullable.Puller == buckleUid)
            {
                // can't pull it and buckle to it at the same time
                _pulling.TryStopPull(toPullable);
            }
        }

        // Logging
        if (userUid != buckleUid)
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(userUid):player} buckled {ToPrettyString(buckleUid)} to {ToPrettyString(strapUid)}");
        else
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(userUid):player} buckled themselves to {ToPrettyString(strapUid)}");

        return true;
    }

    /// <summary>
    /// Tries to unbuckle the Owner of this component from its current strap.
    /// </summary>
    /// <param name="buckleUid">The entity to unbuckle.</param>
    /// <param name="userUid">The entity doing the unbuckling.</param>
    /// <param name="force">
    /// Whether to force the unbuckling or not. Does not guarantee true to
    /// be returned, but guarantees the owner to be unbuckled afterwards.
    /// </param>
    /// <param name="buckleComp">The buckle component of the entity to unbuckle.</param>
    /// <returns>
    ///     true if the owner was unbuckled, otherwise false even if the owner
    ///     was previously already unbuckled.
    /// </returns>
    public bool TryUnbuckle(EntityUid buckleUid, EntityUid userUid, bool force = false, BuckleComponent? buckleComp = null)
    {
        if (!Resolve(buckleUid, ref buckleComp, false) ||
            buckleComp.BuckledTo is not { } strapUid)
            return false;

        if (!force)
        {
            var attemptEvent = new BuckleAttemptEvent(strapUid, buckleUid, userUid, false);
            RaiseLocalEvent(attemptEvent.BuckledEntity, ref attemptEvent);
            RaiseLocalEvent(attemptEvent.StrapEntity, ref attemptEvent);
            if (attemptEvent.Cancelled)
                return false;

            if (_gameTiming.CurTime < buckleComp.BuckleTime + buckleComp.Delay)
                return false;

            if (!_interaction.InRangeUnobstructed(userUid, strapUid, buckleComp.Range, popup: true))
                return false;

            if (HasComp<SleepingComponent>(buckleUid) && buckleUid == userUid)
                return false;

            // If the strap is a vehicle and the rider is not the person unbuckling, return.
            if (TryComp<VehicleComponent>(strapUid, out var vehicle) &&
                vehicle.Rider != userUid)
                return false;
        }

        // Logging
        if (userUid != buckleUid)
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(userUid):player} unbuckled {ToPrettyString(buckleUid)} from {ToPrettyString(strapUid)}");
        else
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(userUid):player} unbuckled themselves from {ToPrettyString(strapUid)}");

        SetBuckledTo(buckleUid, null, null, buckleComp);

        if (!TryComp<StrapComponent>(strapUid, out var strapComp))
            return false;

        var buckleXform = Transform(buckleUid);
        var oldBuckledXform = Transform(strapUid);

        if (buckleXform.ParentUid == strapUid && !Terminating(buckleXform.ParentUid))
        {
            _container.AttachParentToContainerOrGrid((buckleUid, buckleXform));

            var oldBuckledToWorldRot = _transform.GetWorldRotation(strapUid);
            _transform.SetWorldRotation(buckleXform, oldBuckledToWorldRot);

            if (strapComp.UnbuckleOffset != Vector2.Zero)
                buckleXform.Coordinates = oldBuckledXform.Coordinates.Offset(strapComp.UnbuckleOffset);
        }

        if (TryComp(buckleUid, out AppearanceComponent? appearance))
            Appearance.SetData(buckleUid, BuckleVisuals.Buckled, false, appearance);

        if (TryComp<MobStateComponent>(buckleUid, out var mobState)
            && _mobState.IsIncapacitated(buckleUid, mobState)
            || HasComp<KnockedDownComponent>(buckleUid))
        {
            _standing.Down(buckleUid);
        }
        else
        {
            _standing.Stand(buckleUid);
        }

        if (_mobState.IsIncapacitated(buckleUid, mobState))
        {
            _standing.Down(buckleUid);
        }
        if (strapComp.BuckledEntities.Remove(buckleUid))
        {
            strapComp.OccupiedSize -= buckleComp.Size;
            //Dirty(strapUid);
            Dirty(strapComp);
        }

        _joints.RefreshRelay(buckleUid);
        Appearance.SetData(strapUid, StrapVisuals.State, strapComp.BuckledEntities.Count != 0);

        // TODO: Buckle listening to moveevents is sussy anyway.
        if (!TerminatingOrDeleted(strapUid))
            _audio.PlayPredicted(strapComp.UnbuckleSound, strapUid, userUid);

        var ev = new BuckleChangeEvent(strapUid, buckleUid, false);
        RaiseLocalEvent(buckleUid, ref ev);
        RaiseLocalEvent(strapUid, ref ev);

        return true;
    }

    /// <summary>
    /// Makes an entity toggle the buckling status of the owner to a
    /// specific entity.
    /// </summary>
    /// <param name="buckleUid">The entity to buckle/unbuckle from <see cref="to"/>.</param>
    /// <param name="userUid">The entity doing the buckling/unbuckling.</param>
    /// <param name="strapUid">
    /// The entity to toggle the buckle status of the owner to.
    /// </param>
    /// <param name="force">
    /// Whether to force the unbuckling or not, if it happens. Does not
    /// guarantee true to be returned, but guarantees the owner to be
    /// unbuckled afterwards.
    /// </param>
    /// <param name="buckle">The buckle component of the entity to buckle/unbuckle from <see cref="to"/>.</param>
    /// <returns>true if the buckling status was changed, false otherwise.</returns>
    public bool ToggleBuckle(
        EntityUid buckleUid,
        EntityUid userUid,
        EntityUid strapUid,
        bool force = false,
        BuckleComponent? buckle = null)
    {
        if (!Resolve(buckleUid, ref buckle, false))
            return false;

        if (!buckle.Buckled)
        {
            return TryBuckle(buckleUid, userUid, strapUid, buckle);
        }
        else
        {
            return TryUnbuckle(buckleUid, userUid, force, buckle);
        }

    }
}
