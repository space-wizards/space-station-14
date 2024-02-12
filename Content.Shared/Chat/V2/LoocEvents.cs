using Robust.Shared.Serialization;

namespace Content.Shared.Chat.V2;

/// <summary>
/// Raised when a mob tries to speak in LOOC.
/// </summary>
[Serializable, NetSerializable]
public sealed class LoocAttemptedEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public readonly string Message;

    public LoocAttemptedEvent(NetEntity speaker, string message)
    {
        Speaker = speaker;
        Message = message;
    }
}

/// <summary>
/// Raised when a character speaks in LOOC.
/// </summary>
[Serializable, NetSerializable]
public sealed class EntityLoocedEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public string AsName;
    public readonly string Message;
    public float Range;

    public EntityLoocedEvent(NetEntity speaker, string asName, string message, float range)
    {
        Speaker = speaker;
        AsName = asName;
        Message = message;
        Range = range;
    }
}

/// <summary>
/// Raised when a character has failed to speak in LOOC.
/// </summary>
[Serializable, NetSerializable]
public sealed class LoocAttemptFailedEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public readonly string Reason;

    public LoocAttemptFailedEvent(NetEntity speaker, string reason)
    {
        Speaker = speaker;
        Reason = reason;
    }
}

