using Content.Shared.Inventory;

namespace Content.Shared.Gatherable;

/// <summary>
/// Relayed to the user's equipped items when an item has been gathered.
/// </summary>
public record struct ItemGatheredEvent(EntityUid Item, EntityUid User) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;
}
