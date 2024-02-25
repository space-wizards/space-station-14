using Content.Server.Chat.V2.Repository;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Radio;

namespace Content.Server.Chat.V2;

/// <summary>
/// Raised when a comms console makes an announcement.
/// </summary>
public sealed class CommsAnnouncementCreatedEvent : IStorableChatEvent
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

    public string GetMessageType()
    {
        return "Announcement";
    }

    public string GetMessage()
    {
        return Message;
    }
}

/// <summary>
/// Raised locally when a character speaks in Dead Chat.
/// </summary>
public sealed class DeadChatCreatedEvent : IStorableChatEvent
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

    public string GetMessageType()
    {
        return "DeadChat";
    }

    public string GetMessage()
    {
        return Message;
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
public sealed class EmoteCreatedEvent : IStorableChatEvent
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

    public string GetMessageType()
    {
        return "Emote";
    }

    public string GetMessage()
    {
        return Message;
    }
}

/// <summary>
/// A server-only event that is fired when an entity chats in local chat.
/// </summary>
public sealed class LocalChatCreatedEvent : IStorableChatEvent
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

    public string GetMessageType()
    {
        return "LocalChat";
    }

    public string GetMessage()
    {
        return Message;
    }
}

/// <summary>
/// Raised when a character speaks in LOOC.
/// </summary>
public sealed class LoocCreatedEvent : IStorableChatEvent
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

    public string GetMessageType()
    {
        return "LoocChat";
    }

    public string GetMessage()
    {
        return Message;
    }
}

/// <summary>
/// Raised when a character speaks on the radio.
/// </summary>
public sealed class RadioCreatedEvent : IStorableChatEvent
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

    public string GetMessageType()
    {
        return $"Radio:{Channel.Name}";
    }

    public string GetMessage()
    {
        return Message;
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
    public uint Id;

    public RadioEmittedEvent(
        EntityUid speaker,
        string asName,
        string message,
        string channel,
        EntityUid? device,
        uint id
    )
    {
        Speaker = speaker;
        Device = device;
        AsName = asName;
        Message = message;
        Channel = channel;
        Id = id;
    }

    public string GetMessage()
    {
        return Message;
    }
}

/// <summary>
/// Raised when a character whispers.
/// </summary>
public sealed class WhisperCreatedEvent : IStorableChatEvent
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

    public string GetMessageType()
    {
        return "Whisper";
    }

    public string GetMessage()
    {
        return Message;
    }
}

