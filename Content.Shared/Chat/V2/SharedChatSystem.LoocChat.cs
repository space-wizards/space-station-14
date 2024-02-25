using System.Diagnostics.CodeAnalysis;
using Content.Shared.CCVar;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat.V2;

public partial class SharedChatSystem
{
    public bool SendLoocChatMessage(EntityUid speaker, string message, [NotNullWhen(false)] out string? reason)
    {
        if (message.Length > MaxAnnouncementMessageLength)
        {
            reason = Loc.GetString("chat-manager-max-message-length",
                ("maxMessageLength", MaxAnnouncementMessageLength));

            return false;
        }

        RaiseNetworkEvent(new AttemptLoocEvent(GetNetEntity(speaker), message));

        reason = null;

        return true;
    }
}

/// <summary>
/// Raised when a mob tries to speak in LOOC.
/// </summary>
[Serializable, NetSerializable]
public sealed class AttemptLoocEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public readonly string Message;

    public AttemptLoocEvent(NetEntity speaker, string message)
    {
        Speaker = speaker;
        Message = message;
    }
}

/// <summary>
/// Raised when a character has failed to speak in LOOC.
/// </summary>
[Serializable, NetSerializable]
public sealed class LoocFailedEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public readonly string Reason;

    public LoocFailedEvent(NetEntity speaker, string reason)
    {
        Speaker = speaker;
        Reason = reason;
    }
}

/// <summary>
/// Raised when a character speaks in LOOC.
/// </summary>
[Serializable, NetSerializable]
public sealed class LoocEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public string AsName;
    public readonly string Message;
    public uint Id;

    public LoocEvent(NetEntity speaker, string asName, string message, uint id)
    {
        Speaker = speaker;
        AsName = asName;
        Message = message;
        Id = id;
    }
}
