
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Shared.Waypointer;

/// <summary>
/// Whenever a clothing that shows waypointers is equipped.
/// </summary>
[ByRefEvent]
public record struct WaypointerChangedEvent() : IInventoryRelayEvent
{
    public HashSet<ProtoId<WaypointerPrototype>> Waypointers = [];
    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.WITHOUT_POCKET;
}
