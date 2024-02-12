using Robust.Shared.Serialization;

namespace Content.Shared.Chat.V2;

/// <summary>
/// Raised when a mob tries to speak in local chat.
/// </summary>
[Serializable, NetSerializable]
public sealed class LocalChatAttemptedEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public readonly string Message;

    public LocalChatAttemptedEvent(NetEntity speaker, string message)
    {
        Speaker = speaker;
        Message = message;
    }
}

/// <summary>
/// Raised when a character speaks in local chat.
/// </summary>
[Serializable, NetSerializable]
public sealed class EntityLocalChattedEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public string AsName;
    public bool IsBold;
    public string Verb;
    public string FontId;
    public int FontSize;
    public readonly string Message;
    public string AsColor;
    public float Range;
    public bool HideInLog;
    public bool IsSubtle;

    public EntityLocalChattedEvent(NetEntity speaker, string asName, string withVerb, string fontId, int fontSize, bool isBold, string asColor, string message, float range, bool hideInLog, bool isSubtle = false)
    {
        Speaker = speaker;
        AsName = asName;
        Message = message;
        Verb = withVerb;
        FontId = fontId;
        FontSize = fontSize;
        IsBold = isBold;
        AsColor = asColor;
        Range = range;
        HideInLog = hideInLog;
        IsSubtle = isSubtle;
    }
}

/// <summary>
/// Raised when a character has failed to speak in local chat.
/// </summary>
[Serializable, NetSerializable]
public sealed class LocalChatAttemptFailedEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public readonly string Reason;

    public LocalChatAttemptFailedEvent(NetEntity speaker, string reason)
    {
        Speaker = speaker;
        Reason = reason;
    }
}

