using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Database;
using Content.Shared.Hands;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Standing;
using Content.Shared.StatusEffectNew;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared.Stunnable;

public abstract class SharedStunSystem : EntitySystem
{
    public static readonly EntProtoId Stun = "StatusEffectStunned";
    public static readonly EntProtoId Knockdown = "StatusEffectKnockdown";

    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedStatusEffectsSystem _status = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;

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

        SubscribeLocalEvent<StunnedComponent, ComponentStartup>(UpdateCanMove);
        SubscribeLocalEvent<StunnedComponent, ComponentShutdown>(UpdateCanMove);

        SubscribeLocalEvent<StunOnContactComponent, StartCollideEvent>(OnStunOnContactCollide);

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
        SubscribeLocalEvent<StunnedStatusEffectComponent, StatusEffectAppliedEvent>(OnStunEffectApplied);
        SubscribeLocalEvent<StunnedStatusEffectComponent, StatusEffectRemovedEvent>(OnStunStatusRemoved);
        SubscribeLocalEvent<KnockdownStatusEffectComponent, StatusEffectAppliedEvent>(OnKnockdownEffectApplied);
        SubscribeLocalEvent<KnockdownStatusEffectComponent, StatusEffectRemovedEvent>(OnKnockdownEffectRemoved);
    }

    private void OnAttemptInteract(Entity<StunnedComponent> ent, ref InteractionAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnMobStateChanged(EntityUid uid, MobStateComponent component, MobStateChangedEvent args)
    {
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

        TryUpdateStunDuration(args.OtherEntity, ent.Comp.Duration);
        TryUpdateKnockdownDuration(args.OtherEntity, ent.Comp.Duration);
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

    // TODO STUN: Make events for different things. (Getting modifiers, attempt events, informative events...)
    public bool TryAddStunDuration(EntityUid uid, TimeSpan duration)
    {
        if (!_status.TryAddStatusEffectDuration(uid, Stun, duration))
            return false;

        OnStunnedSuccessfully(uid, duration);
        return true;
    }

    public bool TryUpdateStunDuration(EntityUid uid, TimeSpan? duration)
    {
        if (!_status.TryUpdateStatusEffectDuration(uid, Stun, duration))
            return false;

        OnStunnedSuccessfully(uid, duration);
        return true;
    }

    private void OnStunnedSuccessfully(EntityUid uid, TimeSpan? duration)
    {
        var ev = new StunnedEvent(); // todo: rename event or change how it is raised - this event is raised each time duration of stun was externally changed
        RaiseLocalEvent(uid, ref ev);

        var timeForLogs = duration.HasValue
            ? duration.Value.Seconds.ToString()
            : "Infinite";
        _adminLogger.Add(LogType.Stamina, LogImpact.Medium, $"{ToPrettyString(uid):user} stunned for {timeForLogs} seconds");
    }

    public bool TryAddKnockdownDuration(EntityUid uid, TimeSpan duration)
    {
        if (!_status.TryAddStatusEffectDuration(uid, Knockdown, duration))
            return false;

        var ev = new KnockedDownEvent();
        RaiseLocalEvent(uid, ref ev);

        return true;

    }

    public bool TryUpdateKnockdownDuration(EntityUid uid, TimeSpan? duration)
    {
        if (!_status.TryUpdateStatusEffectDuration(uid, Knockdown, duration))
            return false;

        var ev = new KnockedDownEvent();
        RaiseLocalEvent(uid, ref ev);

        return true;
    }

    public bool TryAddParalyzeDuration(EntityUid uid, TimeSpan duration)
    {
        var knockdown = TryAddKnockdownDuration(uid, duration);
        var stunned = TryAddStunDuration(uid, duration);

        return knockdown || stunned;
    }

    public bool TryUpdateParalyzeDuration(EntityUid uid, TimeSpan? duration)
    {
        var knockdown = TryUpdateKnockdownDuration(uid, duration);
        var stunned = TryUpdateStunDuration(uid, duration);

        return knockdown || stunned;
    }

    public bool TryUnstun(Entity<StunnedComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, logMissing: false))
            return true;

        if(!_status.HasEffectComp<StunnedStatusEffectComponent>(entity))
            return false;

        var ev = new StunEndAttemptEvent();
        RaiseLocalEvent(entity, ref ev);

        return !ev.Cancelled && RemComp<StunnedComponent>(entity);
    }

    public bool TryStanding(Entity<StunnedComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, logMissing: false))
            return true;

        if(!_status.HasEffectComp<KnockdownStatusEffectComponent>(entity))
            return false;

        var ev = new KnockdownEndAttemptEvent();
        RaiseLocalEvent(entity, ref ev);

        return !ev.Cancelled && RemComp<KnockedDownComponent>(entity);
    }

    private void OnStunEffectApplied(Entity<StunnedStatusEffectComponent> entity, ref StatusEffectAppliedEvent args)
    {
        EnsureComp<StunnedComponent>(args.Target);
    }

    private void OnStunStatusRemoved(Entity<StunnedStatusEffectComponent> entity, ref StatusEffectRemovedEvent args)
    {
        if(entity.Comp.Remove)
            TryUnstun(args.Target);
    }

    private void OnKnockdownEffectApplied(Entity<KnockdownStatusEffectComponent> entity, ref StatusEffectAppliedEvent args)
    {
        EnsureComp<KnockedDownComponent>(args.Target);
    }

    private void OnKnockdownEffectRemoved(Entity<KnockdownStatusEffectComponent> entity, ref StatusEffectRemovedEvent args)
    {
        // TODO: Remove this when making crawling or else it will break things
        if(entity.Comp.Remove)
            TryStanding(args.Target);
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

/// <summary>
///     Raised on a stunned entity when something wants to remove the stunned component.
/// </summary>
[ByRefEvent]
public record struct StunEndAttemptEvent(bool Cancelled);

/// <summary>
///     Raised on a knocked down entity when something wants to remove the knocked down component.
/// </summary>
[ByRefEvent]
public record struct KnockdownEndAttemptEvent(bool Cancelled);
