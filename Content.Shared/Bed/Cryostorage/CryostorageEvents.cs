using Content.Shared.Inventory;

namespace Content.Shared.Bed.Cryostorage;

[ByRefEvent]
public sealed class EnterCryostorageEvent(EntityUid user) : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = ~SlotFlags.WITHOUT_POCKET;

    public EntityUid User = user;

}
