using Content.Client.Anomaly.UI;
using Content.Client.Items;
using Content.Shared.Anomaly.Components;
using Content.Shared.Containers.ItemSlots;

namespace Content.Client.Anomaly;

/// <summary>
/// Wires up item status logic for <see cref="CorePoweredThrowerComponent"/>.
/// </summary>
/// <seealso cref="AnomalyStatusControl"/>
public sealed class AnomalyItemStatusSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

    public override void Initialize()
    {
        base.Initialize();
        Subs.ItemStatus<CorePoweredThrowerComponent>(
            entity => new AnomalyStatusControl(entity, EntityManager, _itemSlots));
    }
}
