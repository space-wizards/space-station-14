using Content.Shared.Inventory;

namespace Content.Shared.Bed.Cryostorage;

[ByRefEvent]
public sealed class EnterCryostorageEvent : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = ~SlotFlags.WITHOUT_POCKET;
}
