using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Inventory;
using Robust.Shared.Network;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using JetBrains.Annotations;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;

namespace Content.Shared.Slippery;

[UsedImplicitly]
public sealed class SlipperySystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SlipperyComponent, StepTriggerAttemptEvent>(HandleAttemptCollide);
        SubscribeLocalEvent<SlipperyComponent, StepTriggeredEvent>(HandleStepTrigger);
        SubscribeLocalEvent<NoSlipComponent, SlipAttemptEvent>(OnNoSlipAttempt);
        SubscribeLocalEvent<ThrownItemComponent, SlipEvent>(OnThrownSlipAttempt);
        // as long as slip-resistant mice are never added, this should be fine (otherwise a mouse-hat will transfer it's power to the wearer).
        SubscribeLocalEvent<NoSlipComponent, InventoryRelayedEvent<SlipAttemptEvent>>((e, c, ev) => OnNoSlipAttempt(e, c, ev.Args));
    }

    private void HandleStepTrigger(EntityUid uid, SlipperyComponent component, ref StepTriggeredEvent args)
    {
        TrySlip(uid, component, args.Tripper);
    }

    private void HandleAttemptCollide(
        EntityUid uid,
        SlipperyComponent component,
        ref StepTriggerAttemptEvent args)
    {
        args.Continue |= CanSlip(uid, args.Tripper);
    }

    private static void OnNoSlipAttempt(EntityUid uid, NoSlipComponent component, SlipAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnThrownSlipAttempt(EntityUid uid, ThrownItemComponent comp, SlipEvent args)
    {
        args.Cancel();

        //TODO: If it's after 2024 March and this popup is still here, remove it
        // Only here so people who don't read even the changelog won't think soap suddenly broke
        if (_netManager.IsServer)
            _popup.PopupEntity(Loc.GetString("thrown-slippery-missed"), uid, PopupType.Medium);
    }

    private bool CanSlip(EntityUid uid, EntityUid toSlip)
    {
        return !_container.IsEntityInContainer(uid)
                && _statusEffects.CanApplyEffect(toSlip, "Stun"); //Should be KnockedDown instead?
    }

    private void TrySlip(EntityUid uid, SlipperyComponent component, EntityUid other)
    {
        if (HasComp<KnockedDownComponent>(other) && !component.SuperSlippery)
            return;

        var attemptEv = new SlipAttemptEvent();
        RaiseLocalEvent(other, attemptEv);
        if (attemptEv.Cancelled)
            return;

        var ev = new SlipEvent(other);
        RaiseLocalEvent(uid, ev);
        if (ev.Cancelled)
            return;

        if (TryComp(other, out PhysicsComponent? physics) && !HasComp<SlidingComponent>(other))
        {
            _physics.SetLinearVelocity(other, physics.LinearVelocity * component.LaunchForwardsMultiplier, body: physics);

            if (component.SuperSlippery)
            {
                var sliding = EnsureComp<SlidingComponent>(other);
                sliding.CollidingEntities.Add(uid);
                DebugTools.Assert(_physics.GetContactingEntities(other, physics).Contains(uid));
            }
        }

        var playSound = !_statusEffects.HasStatusEffect(other, "KnockedDown");

        _stun.TryParalyze(other, TimeSpan.FromSeconds(component.ParalyzeTime), true);

        // Preventing from playing the slip sound when you are already knocked down.
        if (playSound)
        {
            _audio.PlayPredicted(component.SlipSound, other, other);
        }

        _adminLogger.Add(LogType.Slip, LogImpact.Low,
            $"{ToPrettyString(other):mob} slipped on collision with {ToPrettyString(uid):entity}");
    }
}

/// <summary>
///     Raised on an entity to determine if it can slip or not.
/// </summary>
public sealed class SlipAttemptEvent : CancellableEntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.FEET;
}

/// <summary>
///     Raised on an entity that CAUSED some other entity to slip (e.g., the banana peel).
/// </summary>
public sealed class SlipEvent : CancellableEntityEventArgs
{
    /// <summary>
    /// The person being slipped.
    /// </summary>
    public EntityUid Slipped;
    public SlipEvent(EntityUid slipped)
    {
        Slipped = slipped;
    }
}
