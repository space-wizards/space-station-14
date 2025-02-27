using Content.Shared.Inventory;

namespace Content.Shared.Bed.Cryostorage;


/// <summary>
/// This event is raised just before the players body is removed to cryostorage.
/// Its purpose is to allow for cleanup of the players inventory before they are removed.
/// </summary>
[ByRefEvent]
public sealed class EnterCryostorageEvent(EntityUid user) : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.WITHOUT_POCKET;

    public EntityUid User = user;
}
