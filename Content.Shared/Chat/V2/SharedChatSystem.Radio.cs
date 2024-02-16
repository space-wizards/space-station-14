using System.Diagnostics.CodeAnalysis;
using Content.Shared.CCVar;
using Content.Shared.Chat.V2.Components;
using Content.Shared.Radio;
using Robust.Shared.Serialization;

namespace Content.Shared.Chat.V2;

public partial class SharedChatSystem
{
    public bool SendRadioMessage(EntityUid speaker, string message, RadioChannelPrototype radioChannel, [NotNullWhen(false)] out string? reason)
    {
        if (message.Length > MaxChatMessageLength)
        {
            reason = Loc.GetString("chat-system-max-message-length");

            return false;
        }

        if (TrySendInnateRadioMessage(speaker, message, radioChannel))
        {
            reason = null;

            return true;
        }

        if (!TryComp<HeadsetRadioableComponent>(speaker, out var comp))
        {
            reason = Loc.GetString("chat-system-radio-failed");

            return false;
        }

        if (!comp.Channels.Contains(radioChannel.ID))
        {
            reason = Loc.GetString("chat-system-radio-channel-failed", ("channel", radioChannel.ID));

            return false;
        }

        RaiseNetworkEvent(new AttemptHeadsetRadioEvent(GetNetEntity(speaker), message, radioChannel.ID));

        reason = null;

        return true;
    }

    private bool TrySendInnateRadioMessage(EntityUid speaker, string message, RadioChannelPrototype radioChannel)
    {
        if (!TryComp<InternalRadioComponent>(speaker, out var comp))
        {
            return false;
        }

        if (!comp.SendChannels.Contains(radioChannel.ID))
        {
            return false;
        }

        RaiseNetworkEvent(new AttemptInternalRadioEvent(GetNetEntity(speaker), message, radioChannel.ID));

        return true;
    }
}

/// <summary>
/// Raised when a mob tries to use the radio via a headset or similar device.
/// </summary>
[Serializable, NetSerializable]
public sealed class AttemptHeadsetRadioEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public readonly string Message;
    public readonly string Channel;

    public AttemptHeadsetRadioEvent(NetEntity speaker, string message, string channel)
    {
        Speaker = speaker;
        Message = message;
        Channel = channel;
    }
}

/// <summary>
/// Raised when a mob tries to use the radio via their innate abilities.
/// </summary>
[Serializable, NetSerializable]
public sealed class AttemptInternalRadioEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public readonly string Message;
    public readonly string Channel;

    public AttemptInternalRadioEvent(NetEntity speaker, string message, string channel)
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
public sealed class RadioEvent : EntityEventArgs
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

    public RadioEvent(
        NetEntity speaker,
        string asName,
        string message,
        string channel,
        string withVerb = "",
        string fontId = "",
        int fontSize = 0,
        bool isBold = false,
        bool isAnnouncement = false,
        Color? messageColorOverride = null
    )
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
public sealed class RadioFailedEvent : EntityEventArgs
{
    public NetEntity Speaker;
    public readonly string Reason;

    public RadioFailedEvent(NetEntity speaker, string reason)
    {
        Speaker = speaker;
        Reason = reason;
    }
}
