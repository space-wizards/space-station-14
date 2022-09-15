using Robust.Shared.Serialization;

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
