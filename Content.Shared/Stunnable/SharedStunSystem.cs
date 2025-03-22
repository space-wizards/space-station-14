using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Bed.Sleep;
using Content.Shared.Database;
using Content.Shared.Hands;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Stunnable;

public abstract class SharedStunSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;

    /// <summary>
    /// Friction modifier for knocked down players.
    /// Doesn't make them faster but makes them slow down... slower.
    /// </summary>
    public const float KnockDownModifier = 0.4f;

    public override void Initialize()
    {
        SubscribeLocalEvent<KnockedDownComponent, ComponentInit>(OnKnockInit);
        SubscribeLocalEvent<KnockedDownComponent, ComponentShutdown>(OnKnockShutdown);
        SubscribeLocalEvent<KnockedDownComponent, StandAttemptEvent>(OnStandAttempt);

        SubscribeLocalEvent<SlowedDownComponent, ComponentInit>(OnSlowInit);
        SubscribeLocalEvent<SlowedDownComponent, ComponentShutdown>(OnSlowRemove);

        SubscribeLocalEvent<StunnedComponent, ComponentStartup>(UpdateCanMove);
        SubscribeLocalEvent<StunnedComponent, ComponentShutdown>(UpdateCanMove);

        SubscribeLocalEvent<StunOnContactComponent, ComponentStartup>(OnStunOnContactStartup);
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

    private void OnStunOnContactStartup(Entity<StunOnContactComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<PhysicsComponent>(ent, out var body))
            _broadphase.RegenerateContacts((ent, body));
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
    public bool TryStun(EntityUid uid, TimeSpan time, bool refresh,
        StatusEffectsComponent? status = null)
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
    public bool TryParalyze(EntityUid uid, TimeSpan time, bool refresh,
        StatusEffectsComponent? status = null)
    {
        if (!Resolve(uid, ref status, false))
            return false;

        return TryKnockdown(uid, time, refresh, status) && TryStun(uid, time, refresh, status);
    }

    /// <summary>
    ///     Slows down the mob's walking/running speed temporarily
    /// </summary>
    public bool TrySlowdown(EntityUid uid, TimeSpan time, bool refresh,
        float walkSpeedMultiplier = 1f, float runSpeedMultiplier = 1f,
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
            walkSpeedMultiplier = Math.Clamp(walkSpeedMultiplier, 0f, 1f);
            runSpeedMultiplier = Math.Clamp(runSpeedMultiplier, 0f, 1f);

            slowed.WalkSpeedModifier *= walkSpeedMultiplier;
            slowed.SprintSpeedModifier *= runSpeedMultiplier;

            _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);

            return true;
        }

        return false;
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
