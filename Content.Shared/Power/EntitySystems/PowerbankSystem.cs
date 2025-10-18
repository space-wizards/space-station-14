using System.Linq;
using Content.Shared.Body.Events;
using Content.Shared.Buckle.Components;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Power.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Power.EntitySystems;

public sealed class PowerbankSystem : EntitySystem
{
    [Dependency] private readonly SharedBatterySystem _batterySystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PowerbankComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<PowerbankComponent, PowerbankChargeDoAfterEvent>(OnPowerbankChargeDoAfter);
    }

    private void OnInteract(Entity<PowerbankComponent> ent, ref AfterInteractEvent args)
    {
        if (!TryComp<BatteryComponent>(args.Target, out var targetBattery)
            || targetBattery.CurrentCharge >= targetBattery.MaxCharge) // Check if Target isn't already fully charged.
            return;

        var powerbank = Comp<BatteryComponent>(ent.Owner);

        if (powerbank.CurrentCharge == 0) // Check if the powerbank is empty.
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, ent.Comp.ChargeDelay, new PowerbankChargeDoAfterEvent(), used: args.Used, target: args.Target, eventTarget: args.Used)
        {
            BreakOnMove = false,
            BreakOnDamage = false,
            NeedHand = true,
        });


    }

    private void OnPowerbankChargeDoAfter(Entity<PowerbankComponent> ent, ref PowerbankChargeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled
            || !TryComp<BatteryComponent>(args.Used, out var powerbank)
            || !TryComp<BatteryComponent>(args.Target, out var targetBattery)
            || powerbank.CurrentCharge == 0
            || targetBattery.CurrentCharge >= targetBattery.MaxCharge)
            return;

        var missingAmount = targetBattery.MaxCharge - targetBattery.CurrentCharge;
        // If the missing amount is less than the transfer- or chargeAmount, use the lesser value.
        var chargeAmount = Math.Min(ent.Comp.TransferAmount, Math.Min(missingAmount, powerbank.CurrentCharge));

        var powerbankEntity = (ent.Owner, powerbank);
        var targetEntity = (args.Target.Value, targetBattery);

        _batterySystem.TransferCharge(powerbankEntity, targetEntity, chargeAmount);

        args.Repeat = powerbank.CurrentCharge > 0 && targetBattery.CurrentCharge < targetBattery.MaxCharge;
    }
}

/// <summary>
/// DoAfter event for recharging an entity with the battery component.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class PowerbankChargeDoAfterEvent : SimpleDoAfterEvent;
