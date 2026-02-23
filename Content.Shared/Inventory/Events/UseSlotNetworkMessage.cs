using Robust.Shared.Serialization;

namespace Content.Shared.Inventory.Events;

[NetSerializable, Serializable]
public sealed class UseSlotNetworkMessage(string slot, bool utilityInteraction) : EntityEventArgs
{
    // The slot-owner is implicitly the client that is sending this message.
    // Otherwise clients could start forcefully undressing other clients.
    public readonly string Slot = slot;

    /// <summary>
    /// Whether the slot was interacted with via holding the interaction button.
    /// </summary>
    public readonly bool UtilityInteraction = utilityInteraction;
}
