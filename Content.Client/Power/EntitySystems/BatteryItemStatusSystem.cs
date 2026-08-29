using Content.Client.Items;
using Content.Client.Power.UI;
using Content.Shared.Item;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Power.EntitySystems;

/// <summary>
/// Wires up item status logic for <see cref="BatteryComponent"/> and <see cref="BatteryStatusControl"/>.
/// Shows battery charge information when examining items with batteries.
/// </summary>
public sealed partial class BatteryItemStatusSystem : EntitySystem
{
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;

    public ProtoId<ItemStatusPrototype> BatteryItemStatus = "Battery";
    public ProtoId<ItemStatusPrototype> CellItemStatus = "Cell";

    public override void Initialize()
    {
        base.Initialize();

        Subs.ItemStatus<BatteryComponent>(entity =>
            new BatteryStatusControl(entity.Owner, EntityManager, _battery, _powerCell), BatteryItemStatus);
        Subs.ItemStatus<PowerCellSlotComponent>(entity =>
            new BatteryStatusControl(entity.Owner, EntityManager, _battery, _powerCell), CellItemStatus);
    }
}
