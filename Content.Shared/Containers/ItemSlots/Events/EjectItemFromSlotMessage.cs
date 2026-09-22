using Content.Shared.Containers.ItemSlots.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Containers.ItemSlots.Events;

/// <summary>
/// This is used by the <seealso cref="OpenItemSlotRadialOnInteractSystem"/>.
/// </summary>
/// <param name="slotId"> The slot whose item is to be ejected. </param>
[Serializable, NetSerializable]
public sealed class EjectItemFromSlotMessage(string slotId) : BoundUserInterfaceMessage
{
    /// <summary>
    /// The item slot to eject the item from.
    /// </summary>
    public string SlotId = slotId;
}
