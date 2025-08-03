﻿using Content.Shared.Alert;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Input;
using Content.Shared.Item;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Standing;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
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
    [Dependency] private readonly SharedGunSystem _gunSystem = default!; // 🌟Starlight🌟
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedItemSystem _item = default!; // 🌟Starlight🌟
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;

    private static readonly ProtoId<ItemSizePrototype> MaxItemSize = "Small";

    public static readonly ProtoId<AlertPrototype> KnockdownAlert = "Knockdown";

    private void InitializeKnockdown()
    {
        SubscribeLocalEvent<KnockedDownComponent, RejuvenateEvent>(OnRejuvenate);

        // Startup and Shutdown
        SubscribeLocalEvent<KnockedDownComponent, ComponentInit>(OnKnockInit);
        SubscribeLocalEvent<KnockedDownComponent, ComponentShutdown>(OnKnockShutdown);

        // Action blockers
        SubscribeLocalEvent<KnockedDownComponent, BuckleAttemptEvent>(OnBuckleAttempt);
        SubscribeLocalEvent<KnockedDownComponent, StandAttemptEvent>(OnStandAttempt);
        SubscribeLocalEvent<KnockedDownComponent, ShotAttemptedEvent>(OnShootAttempt); // 🌟Starlight🌟
        SubscribeLocalEvent<KnockedDownComponent, AttemptMeleeEvent>(OnMeleeAttempt); // 🌟Starlight🌟

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

            TryStanding(uid, out knockedDown.DoAfterId);
            DirtyField(uid, knockedDown, nameof(KnockedDownComponent.DoAfterId));
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
        _standingState.Down(entity, true, entity.Comp.AutoStand);
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

    public void ToggleAutoStand(Entity<KnockedDownComponent?> entity, bool autoStand = false)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        entity.Comp.AutoStand = autoStand;
        DirtyField(entity, entity.Comp, nameof(entity.Comp.AutoStand));
    }

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

    public void RefreshKnockdownTime(Entity<KnockedDownComponent> entity, TimeSpan time, bool refresh = true)
    {
        if (refresh)
            UpdateKnockdownTime(entity, time);
        else
            AddKnockdownTime(entity, time);
    }

    public void SetKnockdownTime(Entity<KnockedDownComponent> entity, TimeSpan time)
    {
        entity.Comp.NextUpdate = time;
        DirtyField(entity, entity.Comp, nameof(KnockedDownComponent.NextUpdate));
    }

    public void UpdateKnockdownTime(Entity<KnockedDownComponent> entity, TimeSpan time)
    {
        var knockedTime = GameTiming.CurTime + time;
        if (entity.Comp.NextUpdate < knockedTime)
            SetKnockdownTime(entity, knockedTime);
    }

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
            TryKnockdown(playerEnt, TimeSpan.FromSeconds(0.5), true, false); // TODO: Unhardcode these numbers
            return;
        }

        var stand = !component.DoAfterId.HasValue;

        if (stand)
        {
            TryStanding(playerEnt, out component.DoAfterId);
            DirtyField(playerEnt, component, nameof(KnockedDownComponent.DoAfterId));
        }
        else
            CancelKnockdownDoAfter((playerEnt, component));

        ToggleAutoStand(playerEnt, stand);
    }

    public bool TryStanding(Entity<KnockedDownComponent?, StandingStateComponent?> entity, out ushort? id)
    {
        id = null;
        // If we aren't knocked down or can't be knocked down, then we did technically succeed in standing up
        if (!Resolve(entity, ref entity.Comp1, ref entity.Comp2, false))
            return true;

        id = entity.Comp1.DoAfterId;

        if (!CanStand((entity.Owner, entity.Comp1)))
            return false;

        var ev = new StandUpArgsEvent(entity.Comp2.StandTime);
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

        id = doAfterId.Value.Index;
        return true;
    }

    private bool StandingBlocked(Entity<KnockedDownComponent> entity)
    {
        if (!CanStand(entity))
            return true;

        if (!IntersectingStandingColliders(entity.Owner))
            return false;

        _popup.PopupClient(Loc.GetString("knockdown-component-stand-no-room"), entity, entity, PopupType.SmallCaution);
        ToggleAutoStand(entity.Owner);
        return true;

    }

    private bool CanStand(Entity<KnockedDownComponent> entity)
    {
        if (entity.Comp.NextUpdate > GameTiming.CurTime)
            return false;

        if (!_blocker.CanMove(entity))
            return false;

        var ev = new StandUpAttemptEvent();
        RaiseLocalEvent(entity, ref ev);

        return !ev.Cancelled;
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
        ToggleAutoStand(entity, true);

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
        if (!TryStanding(entity.Owner, out entity.Comp.DoAfterId))
            ForceStandUp(entity!);

        DirtyField(entity, entity.Comp, nameof(KnockedDownComponent.DoAfterId));
        args.Handled = true;
    }

    private bool TryForceStand(Entity<StaminaComponent?> entity)
    {
        // Can't force stand if no Stamina.
        if (!Resolve(entity, ref entity.Comp, false))
            return false;

        var staminaDamage = entity.Comp.ForceStandStamina;

        // TODO: Raise an event to modify the stamina damage?

        if (!Stamina.TryTakeStamina(entity, staminaDamage, entity.Comp, visual: true))
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

        if (args.DamageDelta.GetTotal() >= 5) // TODO: Unhardcode this
            SetKnockdownTime(entity, GameTiming.CurTime + TimeSpan.FromSeconds(0.5f));
    }

    #endregion

    #region Action Blockers

    private void OnStandAttempt(Entity<KnockedDownComponent> entity, ref StandAttemptEvent args)
    {
        if (entity.Comp.LifeStage <= ComponentLifeStage.Running)
            args.Cancel();
    }

    private void OnBuckleAttempt(Entity<KnockedDownComponent> entity, ref BuckleAttemptEvent args)
    {
        if (args.User == entity && entity.Comp.NextUpdate > GameTiming.CurTime)
            args.Cancelled = true;
    }

    // 🌟Starlight🌟
    private void OnShootAttempt(Entity<KnockedDownComponent> entity, ref ShotAttemptedEvent args)
    {
        args.Cancel();
        if (args.Used.Comp.NextFire <= GameTiming.CurTime)
        {
            _popup.PopupClient(Loc.GetString("knockdown-component-shoot-fail"), entity, entity, PopupType.MediumCaution);
            _gunSystem.DelayFire(args.Used!, TimeSpan.FromSeconds(0.5f)); // Same time as a safety delay.
        }
    }

    // 🌟Starlight🌟
    private void OnMeleeAttempt(Entity<KnockedDownComponent> entity, ref AttemptMeleeEvent args)
    {
        // If the weapon is wearable or is our own fists, then we can use it while knocked down
        if (args.Weapon == args.User)
            return;

        if (TryComp<ItemComponent>(args.Weapon, out var item) && _item.GetSizePrototype(item.Size) <= _item.GetSizePrototype(MaxItemSize))
            return;

        args.Cancelled = true;
        _popup.PopupClient(Loc.GetString("knockdown-component-melee-fail"), entity, entity, PopupType.MediumCaution);
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
