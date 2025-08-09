using Content.Shared.Power.Components;
using Content.Client.Power.UI;
using Content.Client.Items;

namespace Content.Client.Power.EntitySystems;

/// <summary>
/// Wires up item status logic for <see cref="SharedBatteryItemComponent"/>.
/// Shows battery charge information when examining items with batteries.
/// </summary>
public sealed class BatteryItemStatusSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        Subs.ItemStatus<SharedBatteryItemComponent>(
            entity => new BatteryStatusControl(entity, EntityManager));
    }
}
