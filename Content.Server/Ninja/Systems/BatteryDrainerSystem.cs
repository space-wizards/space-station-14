using Content.Server.Ninja.Events;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Ninja.Systems;

/// <summary>
/// Handles the doafter and power transfer when draining.
/// </summary>
public sealed class BatteryDrainerSystem : SharedBatteryDrainerSystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BatteryDrainerComponent, BeforeInteractHandEvent>(OnBeforeInteractHand);
        SubscribeLocalEvent<BatteryDrainerComponent, NinjaBatteryChangedEvent>(OnBatteryChanged);
    }

    /// <summary>
    /// Start do after for draining a power source.
    /// Can't predict PNBC existing so only done on server.
    /// </summary>
    private void OnBeforeInteractHand(Entity<BatteryDrainerComponent> ent, ref BeforeInteractHandEvent args)
    {
        var (uid, comp) = ent;
        var target = args.Target;
        if (args.Handled || comp.BatteryUid is not {} battery || !HasComp<PowerNetworkBatteryComponent>(target))
            return;

        // handles even if battery is full so you can actually see the poup
        args.Handled = true;

        if (_battery.IsFull(battery))
        {
            _popup.PopupEntity(Loc.GetString("battery-drainer-full"), uid, uid, PopupType.Medium);
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, uid, comp.DrainTime, new DrainDoAfterEvent(), target: target, eventTarget: uid)
        {
            MovementThreshold = 0.5f,
            BreakOnMove = true,
            CancelDuplicate = false,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnBatteryChanged(Entity<BatteryDrainerComponent> ent, ref NinjaBatteryChangedEvent args)
    {
        SetBattery((ent, ent.Comp), args.Battery);
    }

    /// <inheritdoc/>
    protected override void OnDoAfterAttempt(Entity<BatteryDrainerComponent> ent, ref DoAfterAttemptEvent<DrainDoAfterEvent> args)
    {
        base.OnDoAfterAttempt(ent, ref args);

        if (ent.Comp.BatteryUid is not {} battery || _battery.IsFull(battery))
            args.Cancel();
    }

    /// <inheritdoc/>
    protected override bool TryDrainPower(Entity<BatteryDrainerComponent> ent, EntityUid target)
    {
        var (uid, comp) = ent;
        if (comp.BatteryUid == null || !TryComp<BatteryComponent>(comp.BatteryUid.Value, out var battery))
            return false;

        if (!TryComp<BatteryComponent>(target, out var targetBattery) || !TryComp<PowerNetworkBatteryComponent>(target, out var pnb))
            return false;

        if (MathHelper.CloseToPercent(targetBattery.CurrentCharge, 0))
        {
            _popup.PopupEntity(Loc.GetString("battery-drainer-empty", ("battery", target)), uid, uid, PopupType.Medium);
            return false;
        }

        var available = targetBattery.CurrentCharge;
        var required = battery.MaxCharge - battery.CurrentCharge;
        // higher tier storages can charge more
        var maxDrained = pnb.MaxSupply * comp.DrainTime;
        var input = Math.Min(Math.Min(available, required / comp.DrainEfficiency), maxDrained);
        if (!_battery.TryUseCharge(target, input, targetBattery))
            return false;

        var output = input * comp.DrainEfficiency;
        _battery.SetCharge(comp.BatteryUid.Value, battery.CurrentCharge + output, battery);
        // TODO: create effect message or something
        Spawn("EffectSparks", Transform(target).Coordinates);
        _audio.PlayPvs(comp.SparkSound, target);
        _popup.PopupEntity(Loc.GetString("battery-drainer-success", ("battery", target)), uid, uid);

        // repeat the doafter until battery is full
        return !battery.IsFullyCharged;
    }
}
