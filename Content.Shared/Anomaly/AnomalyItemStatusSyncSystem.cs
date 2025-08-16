using Content.Shared.Anomaly;
using Content.Shared.Anomaly.Components;
using Content.Shared.Containers.ItemSlots;

namespace Content.Shared.Anomaly;

/// <summary>
/// Shared system that updates <see cref="AnomalyItemStatusComponent"/> with current anomaly core status
/// so that it can be displayed on the client item status panel.
/// </summary>
/// <seealso cref="AnomalyStatusControl"/>
public abstract class SharedAnomalyItemStatusSyncSystem : EntitySystem
{
    [Dependency] protected readonly ItemSlotsSystem ItemSlots = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<AnomalyItemStatusComponent, CorePoweredThrowerComponent>();
        while (enumerator.MoveNext(out var uid, out var status, out var thrower))
        {
            bool hasCore = false;
            bool isDecayed = false;
            int charges = 0;

            // Try to get the item in the core slot
            var coreItem = ItemSlots.GetItemOrNull(uid, thrower.CoreSlotId);
            if (coreItem.HasValue)
            {
                // Try to get the anomaly core component
                if (TryComp<AnomalyCoreComponent>(coreItem.Value, out var core))
                {
                    hasCore = true;
                    isDecayed = core.IsDecayed;
                    charges = core.Charge;
                }
            }

            if (status.HasCore != hasCore || status.IsDecayed != isDecayed || status.Charges != charges)
            {
                status.HasCore = hasCore;
                status.IsDecayed = isDecayed;
                status.Charges = charges;
                Dirty(uid, status);
            }
        }
    }
}
