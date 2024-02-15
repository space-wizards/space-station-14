using System.Diagnostics.CodeAnalysis;
using Content.Shared.CCVar;
using Content.Shared.Ghost;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat.V2;

public partial class SharedChatSystem
{
    public bool SendDeadChatMessage(EntityUid speaker, string message, [NotNullWhen(false)] out string? reason)
    {
        // GhostComponent covers "can talk on dead chat" nicely.
        if (_admin.IsAdmin(speaker) && !HasComp<GhostComponent>(speaker) || !_mobState.IsDead(speaker))
        {
            reason = Loc.GetString("chat-system-dead-chat-failed");

            return false;
        }

        if (message.Length > MaxChatMessageLength)
        {
            reason = Loc.GetString("chat-system-max-message-length");

            return false;
        }

        RaiseNetworkEvent(new AttemptDeadChatEvent(GetNetEntity(speaker), message));

        reason = null;

        return true;
    }
}

/// <summary>
/// Raised when a mob tries to speak in dead chat.
/// </summary>
[Serializable, NetSerializable]
public sealed class AttemptDeadChatEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public readonly string Message;

    public AttemptDeadChatEvent(NetEntity speaker, string message)
    {
        Speaker = speaker;
        Message = message;
    }
}

/// <summary>
/// Raised when a character has failed to speak in Dead Chat.
/// </summary>
[Serializable, NetSerializable]
public sealed class DeadChatFailEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public readonly string Reason;

    public DeadChatFailEvent(NetEntity speaker, string reason)
    {
        Speaker = speaker;
        Reason = reason;
    }
}


/// <summary>
/// Raised locally when a character speaks in Dead Chat.
/// </summary>
[Serializable]
public sealed class DeadChatSuccessEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public string AsName;
    public readonly string Message;
    public bool IsAdmin;
    public string Name;

    public DeadChatSuccessEvent(NetEntity speaker, string asName, string message, bool isAdmin, string name)
    {
        Speaker = speaker;
        AsName = asName;
        Message = message;
        IsAdmin = isAdmin;
        Name = name;
    }
}

/// <summary>
/// Raised on the network when a character speaks in dead chat.
/// </summary>
[Serializable, NetSerializable]
public sealed class DeadChatNetworkEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public string AsName;
    public readonly string Message;
    public bool IsAdmin;
    public string Name;

    public DeadChatNetworkEvent(NetEntity speaker, string asName, string message, bool isAdmin, string name)
    {
        Speaker = speaker;
        AsName = asName;
        Message = message;
        IsAdmin = isAdmin;
        Name = name;
    }
}
