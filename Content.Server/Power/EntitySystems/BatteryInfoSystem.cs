using Content.Server.Power.Components;
using Content.Server.PowerCell;
using Content.Shared.Power;
using Content.Shared.PowerCell.Components;

namespace Content.Server.Power.EntitySystems;

/// <summary>
/// Handles battery information requests for UI components.
/// </summary>
public sealed class BatteryInfoSystem : EntitySystem
{
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BatteryComponent, GetBatteryInfoEvent>(OnGetBatteryInfo);
        SubscribeLocalEvent<PowerCellSlotComponent, GetBatteryInfoEvent>(OnGetPowerCellInfo);
    }

    private void OnGetBatteryInfo(Entity<BatteryComponent> entity, ref GetBatteryInfoEvent args)
    {
        var battery = entity.Comp;
        if (battery.MaxCharge > 0)
        {
            args.ChargePercent = battery.CurrentCharge / battery.MaxCharge;
            args.HasBattery = true;
        }
    }

    private void OnGetPowerCellInfo(Entity<PowerCellSlotComponent> entity, ref GetBatteryInfoEvent args)
    {
        if (_powerCell.TryGetBatteryFromSlot(entity.Owner, out var battery, entity.Comp))
        {
            if (battery.MaxCharge > 0)
            {
                args.ChargePercent = battery.CurrentCharge / battery.MaxCharge;
                args.HasBattery = true;
            }
        }
    }
}
