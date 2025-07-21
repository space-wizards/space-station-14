﻿using Content.Shared.Alert;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Standing;
using Robust.Shared.Audio;
using Robust.Shared.Input.Binding;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Stunnable;

/// <summary>
/// This contains the knockdown logic for the stun system for organization purposes.
/// </summary>
public abstract partial class SharedStunSystem
{
    // TODO: Both of these constants need to be moved to a component somewhere, and need to be tweaked for balance...
    // We don't always have standing state available when these are called so it can't go there
    // Maybe I can pass the values to KnockedDownComponent from Standing state on Component init?
    // Default knockdown timer
    public static readonly TimeSpan DefaultKnockedDuration = TimeSpan.FromSeconds(0.5f);
    // Minimum damage taken to refresh our knockdown timer to the default duration
    public static readonly float KnockdownDamageThreshold = 5f;

    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;

    public static readonly ProtoId<AlertPrototype> KnockdownAlert = "Knockdown";

    private void InitializeKnockdown()
    {
        SubscribeLocalEvent<KnockedDownComponent, RejuvenateEvent>(OnRejuvenate);

        // Startup and Shutdown
        SubscribeLocalEvent<KnockedDownComponent, ComponentInit>(OnKnockInit);
        SubscribeLocalEvent<KnockedDownComponent, ComponentShutdown>(OnKnockShutdown);

        // Action blockers
        SubscribeLocalEvent<KnockedDownComponent, BuckleAttemptEvent>(OnBuckleAttempt);
        SubscribeLocalEvent<KnockedDownComponent, StandAttemptEvent>(OnStandUpAttempt);

        // Updating movement a friction
        SubscribeLocalEvent<KnockedDownComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshKnockedSpeed);
        SubscribeLocalEvent<KnockedDownComponent, RefreshFrictionModifiersEvent>(OnRefreshFriction);
        SubscribeLocalEvent<KnockedDownComponent, TileFrictionEvent>(OnKnockedTileFriction);

        // DoAfter event subscriptions
        SubscribeLocalEvent<KnockedDownComponent, TryStandDoAfterEvent>(OnStandDoAfter);

        // Knockdown Extenders
        SubscribeLocalEvent<KnockedDownComponent, DamageChangedEvent>(OnDamaged);

        // Handling Alternative Inputs
        SubscribeAllEvent<ForceStandUpEvent>(OnForceStandup);
        SubscribeLocalEvent<KnockedDownComponent, KnockedDownAlertEvent>(OnKnockedDownAlert);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleKnockdown, InputCmdHandler.FromDelegate(HandleToggleKnockdown, handle: false))
            .Register<SharedStunSystem>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<KnockedDownComponent>();

        while (query.MoveNext(out var uid, out var knockedDown))
        {
            if (!knockedDown.AutoStand || knockedDown.DoAfterId.HasValue || knockedDown.NextUpdate > GameTiming.CurTime)
                continue;

            TryStanding(uid);
        }
    }

    private void OnRejuvenate(Entity<KnockedDownComponent> entity, ref RejuvenateEvent args)
    {
        SetKnockdownTime(entity, GameTiming.CurTime);

        if (entity.Comp.AutoStand)
            RemComp<KnockedDownComponent>(entity);
    }

    #region Startup and Shutdown

    private void OnKnockInit(Entity<KnockedDownComponent> entity, ref ComponentInit args)
    {
        // Other systems should handle dropping held items...
        _standingState.Down(entity, true, false);
        RefreshKnockedMovement(entity);
    }

    private void OnKnockShutdown(Entity<KnockedDownComponent> entity, ref ComponentShutdown args)
    {
        // This is jank but if we don't do this it'll still use the knockedDownComponent modifiers for friction because it hasn't been deleted quite yet.
        entity.Comp.FrictionModifier = 1f;
        entity.Comp.SpeedModifier = 1f;

        _standingState.Stand(entity);
        Alerts.ClearAlert(entity, KnockdownAlert);
    }

    #endregion

    #region API

    /// <summary>
    /// Sets the autostand property of a <see cref="KnockedDownComponent"/> on an entity to true or false and dirties it.
    /// Defaults to false.
    /// </summary>
    /// <param name="entity">Entity we want to edit the data field of.</param>
    /// <param name="autoStand">What we want to set the data field to.</param>
    public void SetAutoStand(Entity<KnockedDownComponent?> entity, bool autoStand = false)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        entity.Comp.AutoStand = autoStand;
        DirtyField(entity, entity.Comp, nameof(KnockedDownComponent.AutoStand));
    }

    /// <summary>
    /// Cancels the DoAfter of an entity with the <see cref="KnockedDownComponent"/> who is trying to stand.
    /// </summary>
    /// <param name="entity">Entity who we are canceling the DoAfter for.</param>
    public void CancelKnockdownDoAfter(Entity<KnockedDownComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        if (entity.Comp.DoAfterId == null)
            return;

        DoAfter.Cancel(entity.Owner, entity.Comp.DoAfterId.Value);
        entity.Comp.DoAfterId = null;
        DirtyField(entity, entity.Comp, nameof(KnockedDownComponent.DoAfterId));
    }

    /// <summary>
    /// Updates the knockdown timer of a knocked down entity with a given inputted time, then dirties the time.
    /// </summary>
    /// <param name="entity">Entity who's knockdown time we're updating.</param>
    /// <param name="time">The time we're updating with.</param>
    /// <param name="refresh">Whether we're resetting the timer or adding to the current timer.</param>
    public void UpdateKnockdownTime(Entity<KnockedDownComponent> entity, TimeSpan time, bool refresh = true)
    {
        if (refresh)
            RefreshKnockdownTime(entity, time);
        else
            AddKnockdownTime(entity, time);
    }

    /// <summary>
    /// Sets the next update datafield of an entity's <see cref="KnockedDownComponent"/> to a specific time.
    /// </summary>
    /// <param name="entity">Entity whose timer we're updating</param>
    /// <param name="time">The exact time we're setting the next update to.</param>
    public void SetKnockdownTime(Entity<KnockedDownComponent> entity, TimeSpan time)
    {
        entity.Comp.NextUpdate = time;
        DirtyField(entity, entity.Comp, nameof(KnockedDownComponent.NextUpdate));
    }

    /// <summary>
    /// Refreshes the amount of time an entity is knocked down to the inputted time, if it is greater than
    /// the current time left.
    /// </summary>
    /// <param name="entity">Entity whose timer we're updating</param>
    /// <param name="time">The time we want them to be knocked down for.</param>
    public void RefreshKnockdownTime(Entity<KnockedDownComponent> entity, TimeSpan time)
    {
        var knockedTime = GameTiming.CurTime + time;
        if (entity.Comp.NextUpdate < knockedTime)
            SetKnockdownTime(entity, knockedTime);
    }

    /// <summary>
    /// Adds our inputted time to an entity's knocked down timer, or sets it to the given time if their timer has expired.
    /// </summary>
    /// <param name="entity">Entity whose timer we're updating</param>
    /// <param name="time">The time we want to add to their knocked down timer.</param>
    public void AddKnockdownTime(Entity<KnockedDownComponent> entity, TimeSpan time)
    {
        if (entity.Comp.NextUpdate < GameTiming.CurTime)
        {
            SetKnockdownTime(entity, GameTiming.CurTime + time);
            return;
        }

        entity.Comp.NextUpdate += time;
        DirtyField(entity, entity.Comp, nameof(KnockedDownComponent.NextUpdate));
    }

    /// <summary>
    /// Checks if an entity is able to stand, returns true if it can, returns false if it cannot.
    /// </summary>
    /// <param name="entity">Entity we're checking</param>
    /// <returns>Returns whether the entity is able to stand</returns>
    public bool CanStand(Entity<KnockedDownComponent> entity)
    {
        if (entity.Comp.NextUpdate > GameTiming.CurTime)
            return false;

        if (!Blocker.CanMove(entity))
            return false;

        var ev = new StandUpAttemptEvent();
        RaiseLocalEvent(entity, ref ev);

        return !ev.Cancelled;
    }

    #endregion

    #region Knockdown Logic

    private void HandleToggleKnockdown(ICommonSession? session)
    {
        if (session is not { } playerSession)
            return;

        if (playerSession.AttachedEntity is not { Valid: true } playerEnt || !Exists(playerEnt))
            return;

        if (!TryComp<KnockedDownComponent>(playerEnt, out var component))
        {
            TryKnockdown(playerEnt, DefaultKnockedDuration, true, false, false); // TODO: Unhardcode these numbers
            return;
        }

        var stand = !component.DoAfterId.HasValue;
        SetAutoStand(playerEnt, stand);

        if (!stand || !TryStanding(playerEnt))
            CancelKnockdownDoAfter((playerEnt, component));
    }

    public bool TryStanding(Entity<KnockedDownComponent?, StandingStateComponent?> entity)
    {
        // If we aren't knocked down or can't be knocked down, then we did technically succeed in standing up
        if (!Resolve(entity, ref entity.Comp1, ref entity.Comp2, false))
            return true;

        if (!TryStand((entity.Owner, entity.Comp1)))
            return false;

        var ev = new GetStandUpTimeEvent(entity.Comp2.StandTime);
        RaiseLocalEvent(entity, ref ev);

        var doAfterArgs = new DoAfterArgs(EntityManager, entity, ev.DoAfterTime, new TryStandDoAfterEvent(), entity, entity)
        {
            BreakOnDamage = true,
            DamageThreshold = 5,
            CancelDuplicate = true,
            RequireCanInteract = false,
            BreakOnHandChange = true
        };

        // If we try standing don't try standing again
        if (!DoAfter.TryStartDoAfter(doAfterArgs, out var doAfterId))
            return false;

        entity.Comp1.DoAfterId = doAfterId.Value.Index;
        DirtyField(entity, entity.Comp1, nameof(KnockedDownComponent.DoAfterId));
        return true;
    }

    /// <summary>
    /// A variant of <see cref="CanStand"/> used when we're actually trying to stand.
    /// Main difference is this one affects autostand datafields and also displays popups.
    /// </summary>
    /// <param name="entity">Entity we're checking</param>
    /// <returns>Returns whether the entity is able to stand</returns>
    public bool TryStand(Entity<KnockedDownComponent> entity)
    {
        if (entity.Comp.NextUpdate > GameTiming.CurTime)
            return false;

        if (!Blocker.CanMove(entity))
            return false;

        var ev = new StandUpAttemptEvent(entity.Comp.AutoStand);
        RaiseLocalEvent(entity, ref ev);

        if (ev.Autostand != entity.Comp.AutoStand)
            SetAutoStand((entity.Owner, entity.Comp), ev.Autostand);

        if (ev.Message != null)
        {
            _popup.PopupClient(ev.Message.Value.Item1, entity, entity, ev.Message.Value.Item2);
        }

        return !ev.Cancelled;
    }

    private bool StandingBlocked(Entity<KnockedDownComponent> entity)
    {
        if (!TryStand(entity))
            return true;

        if (!IntersectingStandingColliders(entity.Owner))
            return false;

        _popup.PopupClient(Loc.GetString("knockdown-component-stand-no-room"), entity, entity, PopupType.SmallCaution);
        SetAutoStand(entity.Owner);
        return true;

    }

    private void OnForceStandup(ForceStandUpEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not {} user)
            return;

        ForceStandUp(user);
    }

    public void ForceStandUp(Entity<KnockedDownComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        // That way if we fail to stand, the game will try to stand for us when we are able to
        SetAutoStand(entity, true);

        if (!HasComp<StandingStateComponent>(entity) || StandingBlocked((entity, entity.Comp)))
            return;

        if (!_hands.TryGetEmptyHand(entity.Owner, out _))
            return;

        if (!TryForceStand(entity.Owner))
            return;

        // If we have a DoAfter, cancel it
        CancelKnockdownDoAfter(entity);
        // Remove Component
        RemComp<KnockedDownComponent>(entity);

        _adminLogger.Add(LogType.Stamina, LogImpact.Medium, $"{ToPrettyString(entity):user} has force stood up from knockdown.");
    }

    private void OnKnockedDownAlert(Entity<KnockedDownComponent> entity, ref KnockedDownAlertEvent args)
    {
        if (args.Handled)
            return;

        // If we're already trying to stand, or we fail to stand try forcing it
        if (!TryStanding(entity.Owner))
            ForceStandUp((entity.Owner, entity.Comp));

        args.Handled = true;
    }

    private bool TryForceStand(Entity<StaminaComponent?> entity)
    {
        // Can't force stand if no Stamina.
        if (!Resolve(entity, ref entity.Comp, false))
            return false;

        var ev = new TryForceStandEvent(entity.Comp.ForceStandStamina);
        RaiseLocalEvent(entity, ref ev);

        if (!Stamina.TryTakeStamina(entity, ev.Stamina, entity.Comp, visual: true))
        {
            _popup.PopupClient(Loc.GetString("knockdown-component-pushup-failure"), entity, entity, PopupType.MediumCaution);
            return false;
        }

        _popup.PopupClient(Loc.GetString("knockdown-component-pushup-success"), entity, entity);
        _audio.PlayPredicted(entity.Comp.ForceStandSuccessSound, entity.Owner, entity.Owner, AudioParams.Default.WithVariation(0.025f).WithVolume(5f));

        return true;
    }

    /// <summary>
    ///     Checks if standing would cause us to collide with something and potentially get stuck.
    ///     Returns true if we will collide with something, and false if we will not.
    /// </summary>
    private bool IntersectingStandingColliders(Entity<TransformComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        var intersecting = _physics.GetEntitiesIntersectingBody(entity, StandingStateSystem.StandingCollisionLayer, false);

        if (intersecting.Count == 0)
            return false;

        var fixtureQuery = GetEntityQuery<FixturesComponent>();
        var xformQuery = GetEntityQuery<TransformComponent>();

        var ourAABB = _entityLookup.GetAABBNoContainer(entity, entity.Comp.LocalPosition, entity.Comp.LocalRotation);

        foreach (var ent in intersecting)
        {
            if (!fixtureQuery.TryGetComponent(ent, out var fixtures))
                continue;

            if (!xformQuery.TryComp(ent, out var xformComp))
                continue;

            var xform = new Transform(xformComp.LocalPosition, xformComp.LocalRotation);

            foreach (var fixture in fixtures.Fixtures.Values)
            {
                if (!fixture.Hard || (fixture.CollisionMask & StandingStateSystem.StandingCollisionLayer) != StandingStateSystem.StandingCollisionLayer)
                    continue;

                for (var i = 0; i < fixture.Shape.ChildCount; i++)
                {
                    var intersection = fixture.Shape.ComputeAABB(xform, i).IntersectPercentage(ourAABB);
                    if (intersection > 0.1f)
                        return true;
                }
            }
        }

        return false;
    }

    #endregion

    #region Knockdown Extenders

    private void OnDamaged(Entity<KnockedDownComponent> entity, ref DamageChangedEvent args)
    {
        // We only want to extend our knockdown timer if it would've prevented us from standing up
        if (!args.InterruptsDoAfters || !args.DamageIncreased || args.DamageDelta == null || GameTiming.ApplyingState)
            return;

        if (args.DamageDelta.GetTotal() >= KnockdownDamageThreshold) // TODO: Unhardcode this
            SetKnockdownTime(entity, GameTiming.CurTime + DefaultKnockedDuration);
    }

    #endregion

    #region Action Blockers

    private void OnStandUpAttempt(Entity<KnockedDownComponent> entity, ref StandAttemptEvent args)
    {
        if (entity.Comp.LifeStage <= ComponentLifeStage.Running)
            args.Cancel();
    }

    private void OnBuckleAttempt(Entity<KnockedDownComponent> entity, ref BuckleAttemptEvent args)
    {
        if (args.User == entity && entity.Comp.NextUpdate > GameTiming.CurTime)
            args.Cancelled = true;
    }

    #endregion

    #region DoAfter

    private void OnStandDoAfter(Entity<KnockedDownComponent> entity, ref TryStandDoAfterEvent args)
    {
        entity.Comp.DoAfterId = null;

        if (args.Cancelled || StandingBlocked(entity))
        {
            DirtyField(entity, entity.Comp, nameof(KnockedDownComponent.DoAfterId));
            return;
        }

        RemComp<KnockedDownComponent>(entity);

        _adminLogger.Add(LogType.Stamina, LogImpact.Medium, $"{ToPrettyString(entity):user} has stood up from knockdown.");
    }

    #endregion

    #region Movement and Friction

    private void RefreshKnockedMovement(Entity<KnockedDownComponent> ent)
    {
        var ev = new KnockedDownRefreshEvent();
        RaiseLocalEvent(ent, ref ev);

        ent.Comp.SpeedModifier = ev.SpeedModifier;
        ent.Comp.FrictionModifier = ev.FrictionModifier;

        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent);
        _movementSpeedModifier.RefreshFrictionModifiers(ent);
    }

    private void OnRefreshKnockedSpeed(Entity<KnockedDownComponent> entity, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(entity.Comp.SpeedModifier);
    }

    private void OnKnockedTileFriction(Entity<KnockedDownComponent> entity, ref TileFrictionEvent args)
    {
        args.Modifier *= entity.Comp.FrictionModifier;
    }

    private void OnRefreshFriction(Entity<KnockedDownComponent> entity, ref RefreshFrictionModifiersEvent args)
    {
        args.ModifyFriction(entity.Comp.FrictionModifier);
        args.ModifyAcceleration(entity.Comp.FrictionModifier);
    }

    #endregion
}
