using System.Diagnostics.CodeAnalysis;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Bed.Sleep;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Input;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Throwing;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Input.Binding;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Stunnable;

public abstract class SharedStunSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBroadphaseSystem _broadphase = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;

    private readonly HashSet<EntityUid> _toUpdate = new();

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
        SubscribeLocalEvent<KnockedDownComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshKnockedSpeed);

        SubscribeLocalEvent<KnockedDownComponent, RefreshFrictionModifiersEvent>(OnRefreshFriction);
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

        // DoAfter event subscriptions
        SubscribeLocalEvent<KnockedDownComponent, TryStandDoAfterEvent>(OnStandDoAfter);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ToggleKnockdown, InputCmdHandler.FromDelegate(HandleToggleKnockdown, handle: false))
            .Register<SharedStunSystem>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _gameTiming.CurTime;

        foreach (var uid in _toUpdate)
        {
            if (HasComp<StunnedComponent>(uid)
                || !TryComp<KnockedDownComponent>(uid, out var knockedDown)
                || knockedDown.NextUpdate >= curTime)
                continue;

            // Check if we're already in a do-after somehow.
            TryStanding(uid, knockedDown);
        }

        /*var enumerator = EntityQueryEnumerator<KnockedDownComponent>();

        while (enumerator.MoveNext(out var uid, out var knockedDown))
        {
            if (HasComp<StunnedComponent>(uid) || knockedDown.AutoStand == false )//|| knockedDown.NextUpdate < curTime)
                continue;

            if (TryComp<DoAfterComponent>(uid, out var comp))
            {

            }

            // Try Standing up
        }*/
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
        _toUpdate.Remove(uid);
        // This is jank but if we don't do this it'll still use the knockeddowncomponent modifiers for friction because it hasn't been deleted quite yet.
        component.FrictionModifier = 1f;
        component.SpeedModifier = 1f;
        _standingState.Stand(uid);
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
        _movementSpeedModifier.RefreshFrictionModifiers(uid);
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

    private void OnRefreshMovespeed(EntityUid ent, SlowedDownComponent comp, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(comp.WalkSpeedModifier, comp.SprintSpeedModifier);
    }

    private void OnRefreshKnockedSpeed(EntityUid ent, KnockedDownComponent comp, RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(comp.SpeedModifier);
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
    public bool TryKnockdown(EntityUid uid, TimeSpan time, bool refresh, StatusEffectsComponent? status = null)
    {
        if (time <= TimeSpan.Zero)
            return false;

        if (!Resolve(uid, ref status, false))
            return false;

        if (!_statusEffect.CanApplyEffect(uid, "KnockedDown", status))
            return false;

        // Don't try to double knockdown and don't force them to get up if they want to stay down.
        if (!EnsureComp<KnockedDownComponent>(uid, out var component) && component.AutoStand)
            _toUpdate.Add(uid);

        component.NextUpdate = _gameTiming.CurTime + time;

        var ev = new KnockedDownEvent();
        RaiseLocalEvent(uid, ref ev);

        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);
        _movementSpeedModifier.RefreshFrictionModifiers(uid);

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

    public bool TryStanding(EntityUid uid, KnockedDownComponent? comp)
    {
        if (!Resolve(uid, ref comp, false))
            return true;

        var doAfterArgs = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(4f), new TryStandDoAfterEvent(), uid, uid)
        {
            BreakOnDamage = true,
            CancelDuplicate = true,
        };

        // If we try to stand up and something cancels it, don't try again
        _toUpdate.Remove(uid);
        return _doAfter.TryStartDoAfter(doAfterArgs);
    }

    public void OnStandDoAfter(EntityUid uid, KnockedDownComponent comp, TryStandDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        RemComp<KnockedDownComponent>(uid);
    }

    private void HandleToggleKnockdown(ICommonSession? session)
    {
        if (session is not { } playerSession)
            return;

        if (playerSession.AttachedEntity is not { Valid: true } playerEnt || !Exists(playerEnt))
            return;

        if (!TryComp<KnockedDownComponent>(playerEnt, out var component))
            TryKnockdown(playerEnt, TimeSpan.FromSeconds(0.5), false);
        else
            TryStanding(playerEnt, component);
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

    private void OnKnockedTileFriction(Entity<KnockedDownComponent> entity, ref TileFrictionEvent args)
    {
        args.Modifier *= entity.Comp.FrictionModifier;
    }

    private void OnRefreshFriction(Entity<KnockedDownComponent> entity, ref RefreshFrictionModifiersEvent args)
    {
        args.ModifyFriction(entity.Comp.FrictionModifier);
        args.ModifyAcceleration(entity.Comp.FrictionModifier);
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

[Serializable, NetSerializable]
public sealed partial class TryStandDoAfterEvent : SimpleDoAfterEvent;
