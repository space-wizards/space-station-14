using Robust.Shared.Serialization;
using Content.Shared.Inventory;

namespace Content.Shared.Chat.TypingIndicator;

/// <summary>
///     Networked event from client.
///     Send to server when client started/stopped typing in chat input field.
/// </summary>
[Serializable, NetSerializable]
public sealed class TypingChangedEvent : EntityEventArgs
{
    public readonly bool IsTyping;

    public TypingChangedEvent(bool isTyping)
    {
        IsTyping = isTyping;
    }
}

[Serializable, NetSerializable]
public sealed class BeforeShowTypingIndicatorEvent : IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.WITHOUT_POCKET;
    public string? OverrideIndicator = null;
    public TimeSpan? LatestEquipTime = null;

    public BeforeShowTypingIndicatorEvent()
    {
        OverrideIndicator = null;
        LatestEquipTime = null;
    }
}
