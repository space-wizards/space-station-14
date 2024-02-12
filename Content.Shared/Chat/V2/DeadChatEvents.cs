using Robust.Shared.Serialization;

namespace Content.Shared.Chat.V2;

/// <summary>
/// Raised when a mob tries to speak in Dead Chat.
/// </summary>
[Serializable, NetSerializable]
public sealed class DeadChatAttemptedEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public readonly string Message;

    public DeadChatAttemptedEvent(NetEntity speaker, string message)
    {
        Speaker = speaker;
        Message = message;
    }
}

/// <summary>
/// Raised when a character speaks in Dead Chat.
/// </summary>
[Serializable, NetSerializable]
public sealed class EntityDeadChattedEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public string AsName;
    public readonly string Message;
    public bool IsAdmin;
    public string Name;

    public EntityDeadChattedEvent(NetEntity speaker, string asName, string message, bool isAdmin, string name)
    {
        Speaker = speaker;
        AsName = asName;
        Message = message;
        IsAdmin = isAdmin;
        Name = name;
    }
}

/// <summary>
/// Raised when a character has failed to speak in Dead Chat.
/// </summary>
[Serializable, NetSerializable]
public sealed class DeadChatAttemptFailedEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public readonly string Reason;

    public DeadChatAttemptFailedEvent(NetEntity speaker, string reason)
    {
        Speaker = speaker;
        Reason = reason;
    }
}

