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
            reason = Loc.GetString("chat-system-max-message-length");

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
public sealed class LocalChatFailedEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public readonly string Reason;

    public LocalChatFailedEvent(NetEntity speaker, string reason)
    {
        Speaker = speaker;
        Reason = reason;
    }
}

/// <summary>
/// Raised to inform clients that an entity has spoken in local chat.
/// </summary>
[Serializable, NetSerializable]
public sealed class LocalChatEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public string AsName;
    public readonly string Message;

    public LocalChatEvent(NetEntity speaker, string asName, string message)
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
public sealed class SubtleChatEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public readonly string Message;

    public SubtleChatEvent(NetEntity speaker, string message)
    {
        Speaker = speaker;
        Message = message;
    }
}

/// <summary>
/// Raised when an entity (such as a vending machine) uses local chat. The chat should not appear in the chat log.
/// </summary>
[Serializable, NetSerializable]
public sealed class BackgroundChatEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public string AsName;
    public readonly string Message;

    public BackgroundChatEvent(NetEntity speaker, string message, string name)
    {
        Speaker = speaker;
        AsName = name;
        Message = message;
    }
}
