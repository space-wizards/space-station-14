using Content.Shared.Chat.Prototypes;

namespace Content.Server.Chat.V2;

public interface IStorableChatEvent
{
    public EntityUid GetSender();
    public void SetId(uint id);
}

/// <summary>
/// Raised when a comms console makes an announcement.
/// </summary>
[ByRefEvent]
public sealed class CommsAnnouncementCreatedEvent : EntityEventArgs, IStorableChatEvent
{
    public uint Id;
    public EntityUid Sender;
    public EntityUid Console;
    public string Message;

    public CommsAnnouncementCreatedEvent(EntityUid sender, EntityUid console, string message)
    {
        Sender = sender;
        Console = console;
        Message = message;
    }

    public EntityUid GetSender()
    {
        return Sender;
    }

    public void SetId(uint id)
    {
        Id = id;
    }
}

/// <summary>
/// Raised locally when a character speaks in Dead Chat.
/// </summary>
[Serializable]
public sealed class DeadChatCreatedEvent : EntityEventArgs, IStorableChatEvent
{
    public uint Id;
    public EntityUid Speaker;
    public string AsName;
    public readonly string Message;
    public bool IsAdmin;

    public DeadChatCreatedEvent(EntityUid speaker, string asName, string message, bool isAdmin)
    {
        Speaker = speaker;
        AsName = asName;
        Message = message;
        IsAdmin = isAdmin;
    }

    public EntityUid GetSender()
    {
        return Speaker;
    }

    public void SetId(uint id)
    {
        Id = id;
    }
}

/// <summary>
/// Raised by chat system when entity made some emote.
/// Use it to play sound, change sprite or something else.
/// </summary>
[ByRefEvent]
public struct EmoteCreatedEvent : IStorableChatEvent
{
    public uint Id;
    public EntityUid Sender;
    public bool Handled;
    public readonly EmotePrototype Emote;

    public EmoteCreatedEvent(EmotePrototype emote)
    {
        Emote = emote;
        Handled = false;
    }

    public EntityUid GetSender()
    {
        return Sender;
    }

    public void SetId(uint id)
    {
        Id = id;
    }
}

/// <summary>
/// A server-only event that is fired when an entity chats in local chat.
/// </summary>
[Serializable]
public sealed class LocalChatCreatedEvent : EntityEventArgs, IStorableChatEvent
{
    public uint Id;
    public EntityUid Speaker;
    public string AsName;
    public readonly string Message;
    public float Range;

    public LocalChatCreatedEvent(EntityUid speaker, string asName, string message, float range)
    {
        Speaker = speaker;
        AsName = asName;
        Message = message;
        Range = range;
    }

    public EntityUid GetSender()
    {
        return Speaker;
    }

    public void SetId(uint id)
    {
        Id = id;
    }
}

/// <summary>
/// Raised when a character speaks in LOOC.
/// </summary>
[Serializable]
public sealed class LoocCreatedEvent : EntityEventArgs, IStorableChatEvent
{
    public uint Id;
    public EntityUid Speaker;
    public string AsName;
    public readonly string Message;

    public LoocCreatedEvent(EntityUid speaker, string asName, string message)
    {
        Speaker = speaker;
        AsName = asName;
        Message = message;
    }

    public EntityUid GetSender()
    {
        return Speaker;
    }

    public void SetId(uint id)
    {
        Id = id;
    }
}

/// <summary>
/// Raised when a character speaks on the radio.
/// </summary>
[Serializable]
public sealed class RadioCreatedEvent : EntityEventArgs, IStorableChatEvent
{
    public uint Id;
    public EntityUid Speaker;
    public EntityUid? Device;
    public string AsName;
    public readonly string Message;
    public readonly string Channel;
    public bool IsBold;
    public string Verb;
    public string FontId;
    public int FontSize;
    public bool IsAnnouncement;
    public Color? MessageColorOverride;

    public RadioCreatedEvent(
        EntityUid speaker,
        string asName,
        string message,
        string channel,
        string withVerb = "",
        string fontId = "",
        int fontSize = 0,
        bool isBold = false,
        bool isAnnouncement = false,
        Color? messageColorOverride = null,
        EntityUid? device = null
    )
    {
        Speaker = speaker;
        Device = device;
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

    public EntityUid GetSender()
    {
        return Speaker;
    }

    public void SetId(uint id)
    {
        Id = id;
    }
}

/// <summary>
/// Raised when a character whispers.
/// </summary>
[Serializable]
public sealed class WhisperCreatedEvent : EntityEventArgs, IStorableChatEvent
{
    public uint Id;
    public EntityUid Speaker;
    public string AsName;
    public readonly string Message;
    public readonly string ObfuscatedMessage;
    public float MinRange;
    public float MaxRange;

    public WhisperCreatedEvent(EntityUid speaker, string asName, float minRange, float maxRange, string message, string obfuscatedMessage)
    {
        Speaker = speaker;
        AsName = asName;
        Message = message;
        ObfuscatedMessage = obfuscatedMessage;
        MinRange = minRange;
        MaxRange = maxRange;
    }

    public EntityUid GetSender()
    {
        return Speaker;
    }

    public void SetId(uint id)
    {
        Id = id;
    }
}
