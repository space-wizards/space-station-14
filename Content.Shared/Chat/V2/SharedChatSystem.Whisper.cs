using System.Diagnostics.CodeAnalysis;
using Content.Shared.CCVar;
using Content.Shared.Chat.V2.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat.V2;

public partial class SharedChatSystem
{
    public bool SendWhisperMessage(EntityUid speaker, string message, [NotNullWhen(false)] out string? reason)
    {
        // Sanity check: if you can't chat you shouldn't be chatting.
        if (!TryComp<WhisperableComponent>(speaker, out _))
        {
            // TODO: Add locstring
            reason = "You can't whisper";

            return false;
        }

        if (message.Length > MaxAnnouncementMessageLength)
        {
            reason = Loc.GetString("chat-manager-max-message-length",
                ("maxMessageLength", MaxAnnouncementMessageLength));

            return false;
        }

        RaiseNetworkEvent(new AttemptWhisperEvent(GetNetEntity(speaker), message));

        reason = null;

        return true;
    }
}

/// <summary>
/// Raised when a mob tries to whisper.
/// </summary>
[Serializable, NetSerializable]
public sealed class AttemptWhisperEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public readonly string Message;

    public AttemptWhisperEvent(NetEntity speaker, string message)
    {
        Speaker = speaker;
        Message = message;
    }
}

/// <summary>
/// Raised when a character whispers.
/// </summary>
[Serializable, NetSerializable]
public sealed class WhisperEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public string AsName;
    public readonly string Message;

    public WhisperEvent(NetEntity speaker, string asName,string message)
    {
        Speaker = speaker;
        AsName = asName;
        Message = message;
    }
}

/// <summary>
/// Raised when a character has failed to whisper.
/// </summary>
[Serializable, NetSerializable]
public sealed class WhisperFailedEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public readonly string Reason;

    public WhisperFailedEvent(NetEntity speaker, string reason)
    {
        Speaker = speaker;
        Reason = reason;
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
