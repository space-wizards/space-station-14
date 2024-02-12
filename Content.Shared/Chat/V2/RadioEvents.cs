using Robust.Shared.Serialization;

namespace Content.Shared.Chat.V2;

/// <summary>
/// Raised when a mob tries to speak in local chat.
/// </summary>
[Serializable, NetSerializable]
public sealed class RadioAttemptedEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public readonly string Message;
    public readonly string Channel;

    public RadioAttemptedEvent(NetEntity speaker, string message, string channel)
    {
        Speaker = speaker;
        Message = message;
        Channel = channel;
    }
}

/// <summary>
/// Raised when a character speaks on the radio.
/// </summary>
[Serializable, NetSerializable]
public sealed class EntityRadioedEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public string AsName;
    public readonly string Message;
    public readonly string Channel;
    public bool IsBold;
    public string Verb;
    public string FontId;
    public int FontSize;
    public bool IsAnnouncement;
    public Color? MessageColorOverride;

    public EntityRadioedEvent(NetEntity speaker,
        string asName,
        string message,
        string channel,
        string withVerb = "",
        string fontId = "",
        int fontSize = 0,
        bool isBold = false,
        bool isAnnouncement = false,
        Color? messageColorOverride = null)
    {
        Speaker = speaker;
        AsName = asName;
        Message = message;
        Channel = channel;
        Verb = withVerb;
        FontId = fontId;
        FontSize = fontSize;
        IsBold = isBold;
        IsAnnouncement = isAnnouncement;
        MessageColorOverride = messageColorOverride;
    }
}

/// <summary>
/// Raised when a character has failed to speak on the radio.
/// </summary>
[Serializable, NetSerializable]
public sealed class RadioAttemptFailedEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public readonly string Reason;

    public RadioAttemptFailedEvent(NetEntity speaker, string reason)
    {
        Speaker = speaker;
        Reason = reason;
    }
}

