using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio;

namespace Content.Server.Ninja.Systems;

public sealed class BatteryDrainerSystem : SharedBatteryDrainerSystem
{
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <inheritdoc/>
    protected override void OnInteract(EntityUid uid, BatteryDrainerComponent comp, InteractionAttemptEvent args)
    {
        var target = args.Target;
        if (comp.BatteryUid == null || !HasComp<PowerNetworkBatteryComponent>(target))
            return;

        // nicer for spam-clicking to not open apc ui, and when draining starts, so cancel the ui action
        args.Cancel();

        if (IsBatteryFull(comp.BatteryUid.Value))
        {
            _popup.PopupEntity(Loc.GetString("ninja-drain-full"), uid, uid, PopupType.Medium);
            return;
        }

        var doAfterArgs = new DoAfterArgs(uid, comp.DrainTime, new DrainDoAfterEvent(), target: target, eventTarget: uid)
        {
            BreakOnUserMove = true,
            MovementThreshold = 0.5f,
            CancelDuplicate = false,
            AttemptFrequency = AttemptFrequency.StartAndEnd,
            // prevent stack overflow /!\
            RequireCanInteract = false
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    /// <inheritdoc/>
    protected override void OnDoAfterAttempt(EntityUid uid, BatteryDrainerComponent comp, DoAfterAttemptEvent<DrainDoAfterEvent> args)
    {
        base.OnDoAfterAttempt(uid, comp, args);

        if (comp.BatteryUid == null || IsBatteryFull(comp.BatteryUid.Value))
            args.Cancel();
    }

    /// <inheritdoc/>
    protected override bool TryDrainPower(EntityUid uid, BatteryDrainerComponent comp, EntityUid target)
    {
        if (comp.BatteryUid == null || !TryComp<BatteryComponent>(comp.BatteryUid.Value, out var battery))
            return false;

        if (!TryComp<BatteryComponent>(target, out var targetBattery) || !TryComp<PowerNetworkBatteryComponent>(target, out var pnb))
            return false;

        if (MathHelper.CloseToPercent(targetBattery.CurrentCharge, 0))
        {
            _popup.PopupEntity(Loc.GetString("ninja-drain-empty", ("battery", target)), uid, uid, PopupType.Medium);
            return false;
        }

        var available = targetBattery.CurrentCharge;
        var required = targetBattery.MaxCharge - targetBattery.CurrentCharge;
        // higher tier storages can charge more
        var maxDrained = pnb.MaxSupply * comp.DrainTime;
        var input = Math.Min(Math.Min(available, required / comp.DrainEfficiency), maxDrained);
        if (_battery.TryUseCharge(target, input, targetBattery))
        {
            var output = input * comp.DrainEfficiency;
            _battery.SetCharge(comp.BatteryUid.Value, battery.CurrentCharge + output, battery);
            Spawn("EffectSparks", Transform(target).Coordinates);
            _audio.PlayPvs(comp.SparkSound, target);

            if (battery.IsFullyCharged)
            {
                _popup.PopupEntity(Loc.GetString("ninja-drain-full"), uid, uid, PopupType.Medium);
                return false;
            }

            _popup.PopupEntity(Loc.GetString("ninja-drain-success", ("battery", target)), uid, uid);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns whether the suit battery is at least 99% charged, basically full.
    /// </summary>
    private bool IsBatteryFull(EntityUid uid)
    {
        if (!TryComp<BatteryComponent>(uid, out var battery))
            return false;

        return battery.CurrentCharge / battery.MaxCharge >= 0.99f;
    }
}
