using Robust.Shared.Serialization;

namespace Content.Shared.Chat.V2;

/// <summary>
/// Raised when a mob tries to whisper.
/// </summary>
[Serializable, NetSerializable]
public sealed class WhisperAttemptedEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public readonly string Message;

    public WhisperAttemptedEvent(NetEntity speaker, string message)
    {
        Speaker = speaker;
        Message = message;
    }
}

/// <summary>
/// Raised when a character whispers.
/// </summary>
[Serializable]
public sealed class EntityWhisperedLocalEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public string AsName;
    public bool IsBold;
    public string FontId;
    public int FontSize;
    /// <summary>
    /// The full version of the message; this is only included if the recipient was inside MinRange of the speaker.
    /// </summary>
    public readonly string Message;
    /// <summary>
    /// The partial version of the message.
    /// </summary>
    public readonly string ObfuscatedMessage;
    public string AsColor;
    public float MinRange;
    public float MaxRange;

    public EntityWhisperedLocalEvent(NetEntity speaker, string asName, string fontId, int fontSize, bool isBold, string asColor, float minRange, float maxRange, string message, string obfuscatedMessage)
    {
        Speaker = speaker;
        AsName = asName;
        Message = message;
        ObfuscatedMessage = obfuscatedMessage;
        FontId = fontId;
        FontSize = fontSize;
        IsBold = isBold;
        AsColor = asColor;
        MinRange = minRange;
        MaxRange = maxRange;
    }
}

/// <summary>
/// Raised when a character whispers.
/// </summary>
[Serializable, NetSerializable]
public sealed class EntityWhisperedEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public string AsName;
    public bool IsBold;
    public string FontId;
    public int FontSize;
    /// <summary>
    /// The full version of the message; this is only included if the recipient was inside MinRange of the speaker.
    /// </summary>
    public readonly string Message;
    public string AsColor;
    public float MinRange;

    public EntityWhisperedEvent(NetEntity speaker, string asName, string fontId, int fontSize, bool isBold, string asColor, float minRange, string message)
    {
        Speaker = speaker;
        AsName = asName;
        Message = message;
        FontId = fontId;
        FontSize = fontSize;
        IsBold = isBold;
        AsColor = asColor;
        MinRange = minRange;
    }
}

/// <summary>
/// Raised when an entity whispered but the receiver is a bit too far away to hear it properly but can see the speaker.
/// </summary>
[Serializable, NetSerializable]
public sealed class EntityWhisperedObfuscatedlyEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public string AsName;
    public bool IsBold;
    public string FontId;
    public int FontSize;
    /// <summary>
    /// The partial version of the message.
    /// </summary>
    public readonly string ObfuscatedMessage;
    public string AsColor;
    public float MaxRange;

    public EntityWhisperedObfuscatedlyEvent(NetEntity speaker, string asName, string fontId, int fontSize, bool isBold, string asColor, float maxRange, string obfuscatedMessage)
    {
        Speaker = speaker;
        AsName = asName;
        FontId = fontId;
        FontSize = fontSize;
        IsBold = isBold;
        AsColor = asColor;
        MaxRange = maxRange;
        ObfuscatedMessage = obfuscatedMessage;
    }
}

/// <summary>
/// Raised when an entity whispered but the receiver is a bit too far away to hear it properly and can't see the speaker.
/// </summary>
[Serializable, NetSerializable]
public sealed class EntityWhisperedTotallyObfuscatedlyEvent : EntityEventArgs
{
    // TODO: This de-facto leaks who's speaking: how can we secure this?
    public NetEntity Speaker;
    public bool IsBold;
    public string FontId;
    public int FontSize;
    /// <summary>
    /// The partial version of the message.
    /// </summary>
    public readonly string ObfuscatedMessage;
    public float MaxRange;

    public EntityWhisperedTotallyObfuscatedlyEvent(NetEntity speaker, string fontId, int fontSize, bool isBold, float maxRange, string obfuscatedMessage)
    {
        Speaker = speaker;
        FontId = fontId;
        FontSize = fontSize;
        IsBold = isBold;
        MaxRange = maxRange;
        ObfuscatedMessage = obfuscatedMessage;
    }
}

/// <summary>
/// Raised when a character has failed to whisper.
/// </summary>
[Serializable, NetSerializable]
public sealed class WhisperAttemptFailedEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public readonly string Reason;

    public WhisperAttemptFailedEvent(NetEntity speaker, string reason)
    {
        Speaker = speaker;
        Reason = reason;
    }
}

