using Content.Shared.Chat.Prototypes;
using Content.Shared.Radio;

namespace Content.Server.Chat.V2;

public interface IStorableChatEvent
{
    public EntityUid GetSender();
    public void SetId(uint id);
    public void PatchMessage(string message);
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

    public void PatchMessage(string message)
    {
        Message = message;
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
    public string Message;
    public bool IsAdmin;

    public DeadChatCreatedEvent(EntityUid speaker, string message, bool isAdmin)
    {
        Speaker = speaker;
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

    public void PatchMessage(string message)
    {
        Message = message;
    }
}

/// <summary>
/// Raised by chat system when entity made some emote.
/// Use it to play sound, change sprite or something else.
/// </summary>
[ByRefEvent]
public struct HandleEmoteEvent
{
    public EntityUid Sender;
    public bool Handled;
    public readonly EmotePrototype Emote;

    public HandleEmoteEvent(EmotePrototype emote)
    {
        Emote = emote;
        Handled = false;
    }
}

/// <summary>
/// Raised by chat system when entity made some emote.
/// Use it to play sound, change sprite or something else.
/// </summary>
[Serializable]
public struct EmoteCreatedEvent : IStorableChatEvent
{
    public uint Id;
    public EntityUid Sender;
    public string Message;
    public float Range;

    public EmoteCreatedEvent(EntityUid sender, string message, float range)
    {
        Sender = sender;
        Message = message;
        Range = range;
    }

    public EntityUid GetSender()
    {
        return Sender;
    }

    public void SetId(uint id)
    {
        Id = id;
    }

    public void PatchMessage(string message)
    {
        Message = message;
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
    public string Message;
    public float Range;

    public LocalChatCreatedEvent(EntityUid speaker, string message, float range)
    {
        Speaker = speaker;
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

    public void PatchMessage(string message)
    {
        Message = message;
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
    public string Message;

    public LoocCreatedEvent(EntityUid speaker, string message)
    {
        Speaker = speaker;
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

    public void PatchMessage(string message)
    {
        Message = message;
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
    public string Message;
    public RadioChannelPrototype Channel;

    public RadioCreatedEvent(
        EntityUid speaker,
        string message,
        RadioChannelPrototype channel
    )
    {
        Speaker = speaker;
        Message = message;
        Channel = channel;
    }

    public EntityUid GetSender()
    {
        return Speaker;
    }

    public void SetId(uint id)
    {
        Id = id;
    }

    public void PatchMessage(string message)
    {
        Message = message;
    }
}

/// <summary>
/// Raised when a character speaks on the radio.
/// </summary>
[Serializable]
public sealed class RadioEmittedEvent : EntityEventArgs
{
    public EntityUid Speaker;
    public EntityUid? Device;
    public string AsName;
    public string Message;
    public string Channel;

    public RadioEmittedEvent(
        EntityUid speaker,
        string asName,
        string message,
        string channel,
        EntityUid? device
    )
    {
        Speaker = speaker;
        Device = device;
        AsName = asName;
        Message = message;
        Channel = channel;
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
    public string Message;
    public float MinRange;
    public float MaxRange;

    public WhisperCreatedEvent(EntityUid speaker, string message, float minRange, float maxRange)
    {
        Speaker = speaker;
        Message = message;
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

    public void PatchMessage(string message)
    {
        Message = message;
    }
}

/// <summary>
/// Raised when a character whispers.
/// </summary>
[Serializable]
public sealed class WhisperEmittedEvent : EntityEventArgs
{
    public EntityUid Speaker;
    public string AsName;
    public string Message;
    public string ObfuscatedMessage;
    public float MinRange;
    public float MaxRange;

    public WhisperEmittedEvent(EntityUid speaker, string asName, float minRange, float maxRange, string message, string obfuscatedMessage)
    {
        Speaker = speaker;
        AsName = asName;
        Message = message;
        ObfuscatedMessage = obfuscatedMessage;
        MinRange = minRange;
        MaxRange = maxRange;
    }
}

