using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using Content.Shared.StatusEffectNew;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Stunnable;

public abstract partial class SharedStunSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _component = default!;
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] protected readonly AlertsSystem Alerts = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] protected readonly SharedDoAfterSystem DoAfter = default!;
    [Dependency] protected readonly SharedStaminaSystem Stamina = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly SharedStatusEffectsSystem _status = default!;

    public static readonly EntProtoId StatusEffectFriciton = "StatusEffectFriction";

    public override void Initialize()
    {
        SubscribeLocalEvent<SlowedDownComponent, ComponentInit>(OnSlowInit);
        SubscribeLocalEvent<SlowedDownComponent, ComponentShutdown>(OnSlowRemove);
        SubscribeLocalEvent<SlowedDownComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);

        SubscribeLocalEvent<FrictionStatusEffectComponent, StatusEffectAppliedEvent>(OnFrictionStatusEffectApplied);
        SubscribeLocalEvent<FrictionStatusEffectComponent, StatusEffectRemovedEvent>(OnFrictionStatusEffectRemoved);
        SubscribeLocalEvent<FrictionStatusModifierComponent, ComponentShutdown>(OnFrictionRemove);
        SubscribeLocalEvent<FrictionStatusModifierComponent, RefreshFrictionModifiersEvent>(OnRefreshFrictionStatus);
        SubscribeLocalEvent<FrictionStatusModifierComponent, TileFrictionEvent>(OnRefreshTileFrictionStatus);

        SubscribeLocalEvent<StunnedComponent, ComponentStartup>(UpdateCanMove);
        SubscribeLocalEvent<StunnedComponent, ComponentShutdown>(UpdateCanMove);

        SubscribeLocalEvent<StunOnContactComponent, StartCollideEvent>(OnStunOnContactCollide);

        // Attempt event subscriptions.
        SubscribeLocalEvent<StunnedComponent, ChangeDirectionAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<StunnedComponent, UpdateCanMoveEvent>(OnMoveAttempt);
        SubscribeLocalEvent<StunnedComponent, InteractionAttemptEvent>(OnAttemptInteract);
        SubscribeLocalEvent<StunnedComponent, UseAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<StunnedComponent, ThrowAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<StunnedComponent, DropAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<StunnedComponent, AttackAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<StunnedComponent, PickupAttemptEvent>(OnAttempt);
        SubscribeLocalEvent<StunnedComponent, IsEquippingAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<StunnedComponent, IsUnequippingAttemptEvent>(OnUnequipAttempt);
        SubscribeLocalEvent<MobStateComponent, MobStateChangedEvent>(OnMobStateChanged);

        InitializeKnockdown();
    }

    private void OnAttemptInteract(Entity<StunnedComponent> ent, ref InteractionAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnMobStateChanged(EntityUid uid, MobStateComponent component, MobStateChangedEvent args)
    {
        if (!TryComp<StatusEffectsComponent>(uid, out var status))
        {
            return;
        }
        switch (args.NewMobState)
        {
            case MobState.Alive:
                {
                    break;
                }
            case MobState.Critical:
                {
                    _statusEffect.TryRemoveStatusEffect(uid, "Stun");
                    break;
                }
            case MobState.Dead:
                {
                    _statusEffect.TryRemoveStatusEffect(uid, "Stun");
                    break;
                }
            case MobState.Invalid:
            default:
                return;
        }

    }

    private void UpdateCanMove(EntityUid uid, StunnedComponent component, EntityEventArgs args)
    {
        _blocker.UpdateCanMove(uid);
    }

    private void OnStunOnContactCollide(Entity<StunOnContactComponent> ent, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != ent.Comp.FixtureId)
            return;

        if (_entityWhitelist.IsBlacklistPass(ent.Comp.Blacklist, args.OtherEntity))
            return;

        if (!TryComp<StatusEffectsComponent>(args.OtherEntity, out var status))
            return;

        TryStun(args.OtherEntity, ent.Comp.Duration, true, status);
        TryKnockdown(args.OtherEntity, ent.Comp.Duration, ent.Comp.Refresh, ent.Comp.AutoStand, status);
    }

    private void OnSlowInit(EntityUid uid, SlowedDownComponent component, ComponentInit args)
    {
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }

    private void OnSlowRemove(EntityUid uid, SlowedDownComponent component, ComponentShutdown args)
    {
        component.SprintSpeedModifier = 1f;
        component.WalkSpeedModifier = 1f;
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
    }

    private void OnFrictionRemove(Entity<FrictionStatusModifierComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.FrictionModifier = 1f;
        ent.Comp.AccelerationModifier = 1f;
        _movementSpeedModifier.RefreshFrictionModifiers(ent);
    }

    // TODO STUN: Make events for different things. (Getting modifiers, attempt events, informative events...)

    /// <summary>
    ///     Stuns the entity, disallowing it from doing many interactions temporarily.
    /// </summary>
    public bool TryStun(EntityUid uid, TimeSpan time, bool refresh, StatusEffectsComponent? status = null)
    {
        if (time <= TimeSpan.Zero)
            return false;

        if (!Resolve(uid, ref status, false))
            return false;

        if (!_statusEffect.TryAddStatusEffect<StunnedComponent>(uid, "Stun", time, refresh))
            return false;

        var ev = new StunnedEvent();
        RaiseLocalEvent(uid, ref ev);

        _adminLogger.Add(LogType.Stamina, LogImpact.Medium, $"{ToPrettyString(uid):user} stunned for {time.Seconds} seconds");
        return true;
    }

    /// <summary>
    ///     Knocks down the entity, making it fall to the ground.
    /// </summary>
    public bool TryKnockdown(EntityUid uid, TimeSpan time, bool refresh, bool autoStand = true, StatusEffectsComponent? status = null)
    {
        if (time <= TimeSpan.Zero)
            return false;

        if (!Resolve(uid, ref status, false))
            return false;

        // Can't fall down if you can't actually be downed.
        if (!HasComp<StandingStateComponent>(uid))
            return false;

        var evAttempt = new KnockDownAttemptEvent()
        {
            AutoStand = autoStand,
        };
        RaiseLocalEvent(uid, ref evAttempt);

        if (evAttempt.Cancelled)
            return false;

        // Initialize our component with the relevant data we need if we don't have it
        if (!TryComp<KnockedDownComponent>(uid, out var component))
        {
            var knockedDown = _component.GetComponent<KnockedDownComponent>();
            knockedDown.AutoStand = evAttempt.AutoStand;
            AddComp(uid, knockedDown);
            component = knockedDown;
        }
        else
        {
            RefreshKnockedMovement((uid, component));

            // TODO: This cancellation does not predict on the client at all
            DoAfter.Cancel(component.DoAfter);
        }

        var knockedEv = new KnockedDownEvent()
        {
            KnockdownTime = time,
        };
        RaiseLocalEvent(uid, ref knockedEv);

        if (refresh)
        {
            var knockedTime = GameTiming.CurTime + knockedEv.KnockdownTime;
            if (TimeSpan.Compare(knockedTime, component.NextUpdate) == 1)
                component.NextUpdate = knockedTime;
        }
        else
            component.NextUpdate += knockedEv.KnockdownTime;

        Dirty(uid, component);

        Alerts.ShowAlert(uid, "Knockdown", null, (GameTiming.CurTime, component.NextUpdate));

        _adminLogger.Add(LogType.Stamina, LogImpact.Medium, $"{ToPrettyString(uid):user} knocked down for {time.Seconds} seconds");

        return true;
    }

    /// <summary>
    ///     Applies knockdown and stun to the entity temporarily.
    /// </summary>
    public bool TryParalyze(EntityUid uid, TimeSpan time, bool refresh,
        StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return false;

        return TryKnockdown(uid, time, refresh, true, status) && TryStun(uid, time, refresh, status);
    }

    /// <summary>
    ///     Slows down the mob's walking/running speed temporarily
    /// </summary>
    public bool TrySlowdown(EntityUid uid, TimeSpan time, bool refresh,
        float walkSpeedMod = 1f, float sprintSpeedMod = 1f,
        StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return false;

        if (time <= TimeSpan.Zero)
            return false;

        if (_statusEffect.TryAddStatusEffect<SlowedDownComponent>(uid, "SlowedDown", time, refresh, status))
        {
            var slowed = Comp<SlowedDownComponent>(uid);
            // Doesn't make much sense to have the "TrySlowdown" method speed up entities now does it?
            walkSpeedMod = Math.Clamp(walkSpeedMod, 0f, 1f);
            sprintSpeedMod = Math.Clamp(sprintSpeedMod, 0f, 1f);

            slowed.WalkSpeedModifier *= walkSpeedMod;
            slowed.SprintSpeedModifier *= sprintSpeedMod;

            _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
            return true;
        }

        return false;
    }

    public bool TryFriction(EntityUid uid,
        TimeSpan time,
        bool refresh,
        float friction,
        float acceleration)
    {
        if (time <= TimeSpan.Zero)
            return false;

        if (!_status.TryAddStatusEffect(uid, StatusEffectFriciton, time, refresh))
            return false;

        if (!_status.TryGetStatusEffect(uid, StatusEffectFriciton, out var status)
            || !TryComp<FrictionStatusEffectComponent>(status, out var frictionStatus))
            return false;

        frictionStatus.FrictionModifier = friction;
        frictionStatus.AccelerationModifier = acceleration;

        UpdateFrictionStatus(uid);

        return true;
    }

    private void UpdateFrictionStatus(Entity<FrictionStatusModifierComponent?> entity, EntityUid? ignore = null)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        if (!_status.TryEffectsWithComp<FrictionStatusEffectComponent>(entity, out var frictionEffects))
            return;

        var modified = false;

        entity.Comp.FrictionModifier = 1f;
        entity.Comp.AccelerationModifier = 1f;

        foreach (var effect in frictionEffects)
        {
            if (effect.Owner == ignore)
                continue;

            modified = true;
            entity.Comp.FrictionModifier *= effect.Comp1.FrictionModifier;
            entity.Comp.AccelerationModifier *= effect.Comp1.AccelerationModifier;
        }

        // Don't need the modifier anymore if it's not actually modifying anything.
        if (!modified)
            RemComp<FrictionStatusModifierComponent>(entity);

        _movementSpeedModifier.RefreshFrictionModifiers(entity);
    }

    /// <summary>
    /// Updates the movement speed modifiers of an entity by applying or removing the <see cref="SlowedDownComponent"/>.
    /// If both walk and run modifiers are approximately 1 (i.e. normal speed) and <see cref="StaminaComponent.StaminaDamage"/> is 0,
    /// or if the both modifiers are 0, the slowdown component is removed to restore normal movement.
    /// Otherwise, the slowdown component is created or updated with the provided modifiers,
    /// and the movement speed is refreshed accordingly.
    /// </summary>
    /// <param name="ent">Entity whose movement speed should be updated.</param>
    /// <param name="walkSpeedModifier">New walk speed modifier. Default is 1f (normal speed).</param>
    /// <param name="runSpeedModifier">New run (sprint) speed modifier. Default is 1f (normal speed).</param>
    public void UpdateStunModifiers(Entity<StaminaComponent?> ent,
        float walkSpeedModifier = 1f,
        float runSpeedModifier = 1f)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (
            (MathHelper.CloseTo(walkSpeedModifier, 1f) && MathHelper.CloseTo(runSpeedModifier, 1f) && ent.Comp.StaminaDamage == 0f) ||
            (walkSpeedModifier == 0f && runSpeedModifier == 0f)
        )
        {
            RemComp<SlowedDownComponent>(ent);
            return;
        }

        EnsureComp<SlowedDownComponent>(ent, out var comp);

        comp.WalkSpeedModifier = walkSpeedModifier;

        comp.SprintSpeedModifier = runSpeedModifier;

        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent);

        Dirty(ent);
    }

    /// <summary>
    /// A convenience overload of <see cref="UpdateStunModifiers(EntityUid, float, float, StaminaComponent?)"/> that sets both
    /// walk and run speed modifiers to the same value.
    /// </summary>
    /// <param name="ent">Entity whose movement speed should be updated.</param>
    /// <param name="speedModifier">New walk and run speed modifier. Default is 1f (normal speed).</param>
    /// <param name="component">
    /// Optional <see cref="StaminaComponent"/> of the entity.
    /// </param>
    public void UpdateStunModifiers(Entity<StaminaComponent?> ent, float speedModifier = 1f)
    {
        UpdateStunModifiers(ent, speedModifier, speedModifier);
    }

    #region friction and movement listeners

    private void OnRefreshMovespeed(EntityUid ent, SlowedDownComponent comp, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(comp.WalkSpeedModifier, comp.SprintSpeedModifier);
    }

    private void OnRefreshFrictionStatus(Entity<FrictionStatusModifierComponent> ent, ref RefreshFrictionModifiersEvent args)
    {
        args.ModifyFriction(ent.Comp.FrictionModifier);
        args.ModifyAcceleration(ent.Comp.AccelerationModifier);
    }

    private void OnRefreshTileFrictionStatus(Entity<FrictionStatusModifierComponent> ent, ref TileFrictionEvent args)
    {
        args.Modifier *= ent.Comp.FrictionModifier;
    }

    private void OnFrictionStatusEffectApplied(Entity<FrictionStatusEffectComponent> entity, ref StatusEffectAppliedEvent args)
    {
        EnsureComp<FrictionStatusModifierComponent>(args.Target);
        // This does not update Friction Modifiers since they may not have been initialized for the status effect yet. You'll need to do that yourself.
    }

    private void OnFrictionStatusEffectRemoved(Entity<FrictionStatusEffectComponent> entity, ref StatusEffectRemovedEvent args)
    {
        // Set modifiers to 1 so that they don't mistakenly get applied when the component refreshes
        UpdateFrictionStatus(args.Target, entity);
    }

    #endregion

    #region Attempt Event Handling

    private void OnMoveAttempt(EntityUid uid, StunnedComponent stunned, UpdateCanMoveEvent args)
    {
        if (stunned.LifeStage > ComponentLifeStage.Running)
            return;

        args.Cancel();
    }

    private void OnAttempt(EntityUid uid, StunnedComponent stunned, CancellableEntityEventArgs args)
    {
        args.Cancel();
    }

    private void OnEquipAttempt(EntityUid uid, StunnedComponent stunned, IsEquippingAttemptEvent args)
    {
        // is this a self-equip, or are they being stripped?
        if (args.Equipee == uid)
            args.Cancel();
    }

    private void OnUnequipAttempt(EntityUid uid, StunnedComponent stunned, IsUnequippingAttemptEvent args)
    {
        // is this a self-equip, or are they being stripped?
        if (args.Unequipee == uid)
            args.Cancel();
    }

    #endregion
}
