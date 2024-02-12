using Robust.Shared.Serialization;

namespace Content.Shared.Chat.V2;

/// <summary>
/// Raised when a mob tries to emote.
/// </summary>
[Serializable, NetSerializable]
public sealed class EmoteAttemptedEvent : EntityEventArgs
{
    public NetEntity Emoter;
    public readonly string Message;

    public EmoteAttemptedEvent(NetEntity emoter, string message)
    {
        Emoter = emoter;
        Message = message;
    }
}

/// <summary>
/// Raised when a mob emotes.
/// </summary>
[Serializable, NetSerializable]
public sealed class EntityEmotedEvent : EntityEventArgs
{
    public NetEntity Emoter;
    public string AsName;
    public readonly string Message;
    public float Range;

    public EntityEmotedEvent(NetEntity emoter, string asName,string message, float range)
    {
        Emoter = emoter;
        AsName = asName;
        Message = message;
        Range = range;
    }
}

/// <summary>
/// Raised when a mob has failed to emote.
/// </summary>
[Serializable, NetSerializable]
public sealed class EmoteAttemptFailedEvent : EntityEventArgs
{
    public NetEntity Emoter;
    public readonly string Reason;

    public EmoteAttemptFailedEvent(NetEntity emoter, string reason)
    {
        Emoter = emoter;
        Reason = reason;
    }
}

