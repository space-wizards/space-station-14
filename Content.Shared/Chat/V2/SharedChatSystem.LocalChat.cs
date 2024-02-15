using System.Diagnostics.CodeAnalysis;
using Content.Shared.CCVar;
using Content.Shared.Chat.V2.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat.V2;

public partial class SharedChatSystem
{
    public bool SendLocalChatMessage(EntityUid speaker, string message, [NotNullWhen(false)] out string? reason)
    {
        if (!TryComp<LocalChattableComponent>(speaker, out _))
        {
            reason = Loc.GetString("chat-system-local-chat-failed");

            return false;
        }

        if (message.Length > MaxChatMessageLength)
        {
            reason = Loc.GetString("chat-manager-max-message-length", ("maxMessageLength", MaxChatMessageLength));

            return false;
        }

        RaiseNetworkEvent(new AttemptLocalChatEvent(GetNetEntity(speaker), message));

        reason = null;

        return true;
    }
}

/// <summary>
/// Raised when a mob tries to speak in local chat.
/// </summary>
[Serializable, NetSerializable]
public sealed class AttemptLocalChatEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public readonly string Message;

    public AttemptLocalChatEvent(NetEntity speaker, string message)
    {
        Speaker = speaker;
        Message = message;
    }
}

/// <summary>
/// Raised when a character has failed to speak in local chat.
/// </summary>
[Serializable, NetSerializable]
public sealed class LocalChatFailEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public readonly string Reason;

    public LocalChatFailEvent(NetEntity speaker, string reason)
    {
        Speaker = speaker;
        Reason = reason;
    }
}

/// <summary>
/// A server-only event that is fired when an entity chats in local chat.
/// </summary>
[Serializable]
public sealed class LocalChatSuccessEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public string AsName;
    public readonly string Message;
    public float Range;

    public LocalChatSuccessEvent(NetEntity speaker, string asName, string message, float range)
    {
        Speaker = speaker;
        AsName = asName;
        Message = message;
        Range = range;
    }
}

/// <summary>
/// Raised to inform clients that an entity has spoken in local chat.
/// </summary>
[Serializable, NetSerializable]
public sealed class LocalChatNetworkEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public string AsName;
    public readonly string Message;

    public LocalChatNetworkEvent(NetEntity speaker, string asName, string message)
    {
        Speaker = speaker;
        AsName = asName;
        Message = message;
    }
}

/// <summary>
/// Raised when a character is given a subtle message in local chat.
/// </summary>
[Serializable, NetSerializable]
public sealed class SubtleChatNetworkEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public readonly string Message;

    public SubtleChatNetworkEvent(NetEntity speaker, string message)
    {
        Speaker = speaker;
        Message = message;
    }
}

/// <summary>
/// Raised when an entity (such as a vending machine) uses local chat. The chat should not appear in the chat log.
/// </summary>
[Serializable, NetSerializable]
public sealed class BackgroundChatNetworkEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public string AsName;
    public readonly string Message;

    public BackgroundChatNetworkEvent(NetEntity speaker, string message, string name)
    {
        Speaker = speaker;
        AsName = name;
        Message = message;
    }
}
