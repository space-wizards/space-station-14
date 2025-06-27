using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.Hands;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
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

namespace Content.Shared.Stunnable;

public abstract class SharedStunSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedStatusEffectsSystem _status = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;

    public static readonly EntProtoId Slowdown = "StatusEffectSlowdown";
    public static readonly EntProtoId Stamina = "StatusEffectStamina";
    public static readonly EntProtoId Stun = "StatusEffectStunned";

    /// <summary>
    /// Friction modifier for knocked down players.
    /// Doesn't make them faster but makes them slow down... slower.
    /// </summary>
    public const float KnockDownModifier = 0.2f;

    public override void Initialize()
    {
        SubscribeLocalEvent<KnockedDownComponent, ComponentInit>(OnKnockInit);
        SubscribeLocalEvent<KnockedDownComponent, ComponentShutdown>(OnKnockShutdown);
        SubscribeLocalEvent<KnockedDownComponent, StandAttemptEvent>(OnStandAttempt);

        SubscribeLocalEvent<SlowedDownComponent, ComponentInit>(OnSlowInit);
        SubscribeLocalEvent<SlowedDownComponent, ComponentShutdown>(OnSlowRemove);

        SubscribeLocalEvent<StunnedComponent, ComponentStartup>(UpdateCanMove);
        SubscribeLocalEvent<StunnedComponent, ComponentShutdown>(UpdateCanMove);

        SubscribeLocalEvent<StunOnContactComponent, StartCollideEvent>(OnStunOnContactCollide);

        // helping people up if they're knocked down
        SubscribeLocalEvent<KnockedDownComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<SlowedDownComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);

        SubscribeLocalEvent<KnockedDownComponent, TileFrictionEvent>(OnKnockedTileFriction);

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

        // New Status Effect subscriptions
        SubscribeLocalEvent<SlowdownStatusEffectComponent, StatusEffectAppliedEvent>(OnSlowStatusApplied);
        SubscribeLocalEvent<SlowdownStatusEffectComponent, StatusEffectRemovedEvent>(OnSlowStatusRemoved);
        SubscribeLocalEvent<StunnedStatusEffectComponent, StatusEffectAppliedEvent>(OnStunEffectApplied);
        SubscribeLocalEvent<StunnedStatusEffectComponent, StatusEffectRemovedEvent>(OnStunStatusRemoved);
    }

    private void OnAttemptInteract(Entity<StunnedComponent> ent, ref InteractionAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnMobStateChanged(EntityUid uid, MobStateComponent component, MobStateChangedEvent args)
    {
        if (!HasComp<StatusEffectsComponent>(uid))
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
                    _status.TryRemoveStatusEffect(uid, Stun);
                    break;
                }
            case MobState.Dead:
                {
                    _status.TryRemoveStatusEffect(uid, Stun);
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

        TryStun(args.OtherEntity, ent.Comp.Duration, true);
        TryKnockdown(args.OtherEntity, ent.Comp.Duration, true, status);
    }

    private void OnKnockInit(EntityUid uid, KnockedDownComponent component, ComponentInit args)
    {
        _standingState.Down(uid);
    }

    private void OnKnockShutdown(EntityUid uid, KnockedDownComponent component, ComponentShutdown args)
    {
        _standingState.Stand(uid);
    }

    private void OnStandAttempt(EntityUid uid, KnockedDownComponent component, StandAttemptEvent args)
    {
        if (component.LifeStage <= ComponentLifeStage.Running)
            args.Cancel();
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

    private void OnRefreshMovespeed(EntityUid uid, SlowedDownComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(component.WalkSpeedModifier, component.SprintSpeedModifier);
    }

    // TODO STUN: Make events for different things. (Getting modifiers, attempt events, informative events...)

    /// <summary>
    ///     Stuns the entity, disallowing it from doing many interactions temporarily.
    /// </summary>
    public bool TryStun(EntityUid uid, TimeSpan time, bool refresh)
    {
        if (time <= TimeSpan.Zero)
            return false;

        if (!_status.TryAddStatusEffect(uid, Stun, time, refresh))
            return false;

        var ev = new StunnedEvent();
        RaiseLocalEvent(uid, ref ev);

        _adminLogger.Add(LogType.Stamina, LogImpact.Medium, $"{ToPrettyString(uid):user} stunned for {time.Seconds} seconds");
        return true;
    }

    /// <summary>
    ///     Knocks down the entity, making it fall to the ground.
    /// </summary>
    public bool TryKnockdown(EntityUid uid, TimeSpan time, bool refresh,
        StatusEffectsComponent? status = null)
    {
        if (time <= TimeSpan.Zero)
            return false;

        if (!Resolve(uid, ref status, false))
            return false;

        if (!_statusEffect.TryAddStatusEffect<KnockedDownComponent>(uid, "KnockedDown", time, refresh))
            return false;

        var ev = new KnockedDownEvent();
        RaiseLocalEvent(uid, ref ev);

        return true;
    }

    /// <summary>
    ///     Applies knockdown and stun to the entity temporarily.
    /// </summary>
    public bool TryParalyze(EntityUid uid,
        TimeSpan time,
        bool refresh,
        StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return false;

        return TryKnockdown(uid, time, refresh, status) && TryStun(uid, time, refresh);
    }

    /// <summary>
    ///     Slows down the mob's walking/running speed temporarily
    /// </summary>
    public bool TrySlowdown(EntityUid uid,
        TimeSpan time,
        bool refresh,
        float speedModifier)
    {
        return TrySlowdown(uid, time, refresh, speedModifier, speedModifier);
    }

    /// <summary>
    ///     Slows down the mob's walking/running speed temporarily
    /// </summary>
    public bool TrySlowdown(EntityUid uid,
        TimeSpan time,
        bool refresh,
        float walkSpeedModifier,
        float sprintSpeedModifier)
    {
        if (time <= TimeSpan.Zero)
            return false;

        if (!_status.TryAddStatusEffect(uid, Slowdown, time, refresh))
            return false;

        if (!_status.TryGetStatusEffect(uid, Slowdown, out var status)
            || !TryComp<SlowdownStatusEffectComponent>(status, out var slowedStatus))
            return false;

        slowedStatus.SprintSpeedModifier = sprintSpeedModifier;
        slowedStatus.WalkSpeedModifier = walkSpeedModifier;

        TryUpdateSlowedStatus(uid);

        return true;
    }

    /// <summary>
    /// Updates the <see cref="SlowedDownComponent"/> speed modifiers and returns true if speed is being modified,
    /// and false if speed isn't being modified.
    /// </summary>
    /// <param name="entity">Entity whose component we're updating</param>
    /// <param name="ignore">Status effect entity we're ignoring for calculating the comp's modifiers.</param>
    private bool TryUpdateSlowedStatus(Entity<SlowedDownComponent?> entity, EntityUid? ignore = null)
    {
        if (!Resolve(entity, ref entity.Comp, logMissing: false))
            return false;

        if (!_status.TryEffectsWithComp<SlowdownStatusEffectComponent>(entity, out var slowEffects))
            return false;

        // If it's not modifying anything then we don't need it
        var modified = false;

        entity.Comp.WalkSpeedModifier = 1f;
        entity.Comp.SprintSpeedModifier = 1f;

        foreach (var effect in slowEffects)
        {
            if (effect == ignore)
                continue;

            modified = true;
            entity.Comp.WalkSpeedModifier *= effect.Comp1.WalkSpeedModifier;
            entity.Comp.SprintSpeedModifier *= effect.Comp1.SprintSpeedModifier;
        }

        _movementSpeedModifier.RefreshMovementSpeedModifiers(entity);

        return modified;
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
    /// <param name="sprintSpeedModifier">New run (sprint) speed modifier. Default is 1f (normal speed).</param>
    public void UpdateStaminaModifiers(Entity<StaminaComponent?> ent,
        float walkSpeedModifier,
        float sprintSpeedModifier)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (MathHelper.CloseTo(walkSpeedModifier, 1f) && MathHelper.CloseTo(sprintSpeedModifier, 1f))
        {
            // Don't add it if it won't do anything
            if (_status.TryGetStatusEffect(ent, Stamina, out _))
                _status.TryRemoveStatusEffect(ent, Stamina);

            return;
        }

        if (!_status.TryAddStatusEffect(ent, Stamina, out var status))
            return;

        if (!TryComp<SlowdownStatusEffectComponent>(status, out var slowedStatus))
            return;

        slowedStatus.SprintSpeedModifier = sprintSpeedModifier;
        slowedStatus.WalkSpeedModifier = walkSpeedModifier;

        TryUpdateSlowedStatus(ent.Owner);

        Dirty(ent);
    }

    /// <summary>
    /// A convenience overload that sets both walk and run speed modifiers to the same value.
    /// </summary>
    /// <param name="ent">Entity whose movement speed should be updated.</param>
    /// <param name="speedModifier">New walk and run speed modifier. Default is 1f (normal speed).</param>
    public void UpdateStaminaModifiers(Entity<StaminaComponent?> ent, float speedModifier = 1f)
    {
        UpdateStaminaModifiers(ent, speedModifier, speedModifier);
    }

    private void OnInteractHand(EntityUid uid, KnockedDownComponent knocked, InteractHandEvent args)
    {
        if (args.Handled || knocked.HelpTimer > 0f)
            return;

        // TODO: This should be an event.
        if (HasComp<SleepingComponent>(uid))
            return;

        // Set it to half the help interval so helping is actually useful...
        knocked.HelpTimer = knocked.HelpInterval / 2f;

        _statusEffect.TryRemoveTime(uid, "KnockedDown", TimeSpan.FromSeconds(knocked.HelpInterval));
        _audio.PlayPredicted(knocked.StunAttemptSound, uid, args.User);
        Dirty(uid, knocked);

        args.Handled = true;
    }

    private void OnStunEffectApplied(Entity<StunnedStatusEffectComponent> entity, ref StatusEffectAppliedEvent args)
    {
        EnsureComp<StunnedComponent>(args.Target);
    }

    private void OnStunStatusRemoved(Entity<StunnedStatusEffectComponent> entity, ref StatusEffectRemovedEvent args)
    {
        if(!_status.TryEffectsWithComp<StunnedStatusEffectComponent>(entity, out _))
            RemComp<StunnedComponent>(args.Target);
    }

    private void OnSlowStatusApplied(Entity<SlowdownStatusEffectComponent> entity, ref StatusEffectAppliedEvent args)
    {
        EnsureComp<SlowedDownComponent>(args.Target);
    }

    private void OnSlowStatusRemoved(Entity<SlowdownStatusEffectComponent> entity, ref StatusEffectRemovedEvent args)
    {
        if (!TryUpdateSlowedStatus(args.Target, entity))
            RemComp<SlowedDownComponent>(args.Target);
    }

    private void OnKnockedTileFriction(EntityUid uid, KnockedDownComponent component, ref TileFrictionEvent args)
    {
        args.Modifier *= KnockDownModifier;
    }

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

/// <summary>
///     Raised directed on an entity when it is stunned.
/// </summary>
[ByRefEvent]
public record struct StunnedEvent;

/// <summary>
///     Raised directed on an entity when it is knocked down.
/// </summary>
[ByRefEvent]
public record struct KnockedDownEvent;
