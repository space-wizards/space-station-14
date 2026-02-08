using Content.Shared.PowerCell;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Power.Components;
using Content.Shared.PowerCell.Components;
using Content.Client.Power.UI;
using Content.Client.Items;

namespace Content.Client.Power.EntitySystems;

/// <summary>
/// Wires up item status logic for <see cref="BatteryComponent"/> and <see cref="BatteryStatusControl"/>.
/// Shows battery charge information when examining items with batteries.
/// </summary>
public sealed class BatteryItemStatusSystem : EntitySystem
{
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.ItemStatus<BatteryComponent>(entity =>
            new BatteryStatusControl(entity.Owner, EntityManager, _battery, _powerCell));
        Subs.ItemStatus<PowerCellSlotComponent>(entity =>
            new BatteryStatusControl(entity.Owner, EntityManager, _battery, _powerCell));
    }
}
