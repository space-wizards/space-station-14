using Robust.Shared.Serialization;

namespace Content.Shared.TypingIndicator;

[Serializable, NetSerializable]
public enum TypingStatus : byte
{
    Typing,
    Idle,
    None
}

/// <summary>
///     Networked event from client.
///     Send to server when client changes typing status
/// </summary>
[Serializable, NetSerializable]
public sealed class TypingChangedEvent : EntityEventArgs
{
    public readonly TypingStatus Status;

    public TypingChangedEvent(TypingStatus status)
    {
        Status = status;
    }
}
