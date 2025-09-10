using Content.Shared.Inventory;

namespace Content.Shared.Slippery;
[ByRefEvent]
public record struct GetSlowedOverSlipperyModifierEvent() : IInventoryRelayEvent
{
    SlotFlags IInventoryRelayEvent.TargetSlots => ~SlotFlags.POCKET;

    public float SlowdownModifier = 1f;
}
