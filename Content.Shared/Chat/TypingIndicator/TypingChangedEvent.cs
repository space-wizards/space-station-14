using Robust.Shared.Serialization;

namespace Content.Shared.Chat.TypingIndicator;

/// <summary>
///     Networked event from client.
///     Send to server when client started/stopped typing in chat input field.
/// </summary>
[Serializable, NetSerializable]
public sealed class TypingChangedEvent : EntityEventArgs
{
    // Corvax-TypingIndicator-Start
    public readonly TypingIndicatorState State;

    public TypingChangedEvent(TypingIndicatorState state)
    {
        State = state;
    }
    // Corvax-TypingIndicator-End
}
