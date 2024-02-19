using System.Diagnostics.CodeAnalysis;
using Content.Server.Power.Components;
using Content.Server.Radio;
using Content.Server.Radio.Components;
using Content.Server.VoiceMask;
using Content.Shared.CCVar;
using Content.Shared.Chat.V2;
using Content.Shared.Chat.V2.Components;
using Content.Shared.Database;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Speech;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Chat.V2;

public sealed partial class ChatSystem
{
    public void InitializeServerRadio()
    {
        SubscribeNetworkEvent<AttemptHeadsetRadioEvent>((msg, args) => { HandleAttemptRadioMessage(args.SenderSession, msg.Speaker, msg.Message, msg.Channel, false); });
        SubscribeNetworkEvent<AttemptInternalRadioEvent>((msg, args) => { HandleAttemptRadioMessage(args.SenderSession, msg.Speaker, msg.Message, msg.Channel, true); });
    }

    private void HandleAttemptRadioMessage(ICommonSession player, NetEntity entity, string message, string channel, bool isInnate)
    {
        var entityUid = GetEntity(entity);

        if (player.AttachedEntity != entityUid)
        {
            return;
        }

        if (IsRateLimited(entityUid, out var reason))
        {
            RaiseNetworkEvent(new RadioFailedEvent(entity, reason), player);

            return;
        }

        HashSet<string> channels;

        if (isInnate)
        {
            if (!TryComp<InternalRadioComponent>(entityUid, out var comp))
            {
                RaiseNetworkEvent(new RadioFailedEvent(entity, "You can't talk on any radio channel."), player);

                return;
            }

            channels = comp.SendChannels;
        }
        else
        {
            if (!TryComp<HeadsetRadioableComponent>(entityUid, out var comp))
            {
                RaiseNetworkEvent(new RadioFailedEvent(entity, "You can't talk on any radio channel."), player);

                return;
            }

            channels = comp.Channels;
        }

        if (!channels.Contains(channel))
        {
            RaiseNetworkEvent(new RadioFailedEvent(entity, Loc.GetString("chat-system-radio-channel-failed", ("channel", channel))), player);

            return;
        }

        if (!_proto.TryIndex(channel, out RadioChannelPrototype? radioChannelProto))
        {
            RaiseNetworkEvent(new RadioFailedEvent(entity, Loc.GetString("chat-system-radio-channel-nonexistent", ("channel", channel))), player);

            return;
        }

        if (message.Length > MaxChatMessageLength)
        {
            RaiseNetworkEvent(new RadioFailedEvent(entity, Loc.GetString("chat-system-max-message-length")),player);

            return;
        }

        SendRadioMessageWithWhisper(entityUid, message, radioChannelProto);
    }

    public void SendRadioMessageViaDevice(EntityUid entityUid, string message, RadioChannelPrototype channel, EntityUid device, string asName = "")
    {
        if (!TryBuildSuccessEvent(entityUid, ref message, channel, ref asName, out var msgOut, device))
            return;

        TransmitToReceivers(entityUid, message, channel, asName, msgOut);
    }

    /// <summary>
    /// Send a radio message via a channel, whispering if needed.
    /// </summary>
    /// <param name="entityUid">The entity who is chatting</param>
    /// <param name="message">The message to send. This will be mutated with accents, to remove tags, etc.</param>
    /// <param name="channel">The channel the message can be heard in</param>
    /// <param name="asName">Override the name this entity will appear as.</param>
    public void SendRadioMessageWithWhisper(EntityUid entityUid, string message, RadioChannelPrototype channel, string asName = "")
    {
        // If you don't have intrinsic radio, you need to whisper to send a message using your voice box.
        if (!TryComp<InternalRadioComponent>(entityUid, out var comp) || !comp.SendChannels.Contains(channel.ID))
        {
            TrySendWhisperMessage(entityUid, message, asName);
        }

        SendRadioMessage(entityUid, message, channel, asName);
    }

    /// <summary>
    /// Send a radio message via a channel to specific targets.
    /// </summary>
    /// <param name="entityUid">The entity who is chatting</param>
    /// <param name="message">The message to send. This will be mutated with accents, to remove tags, etc.</param>
    /// <param name="channel">The channel the message can be heard in</param>
    /// <param name="asName">Override the name this entity will appear as.</param>
    public void SendRadioMessageToTargets(EntityUid entityUid, string message, RadioChannelPrototype channel, Filter allowList, string asName = "")
    {
        if (!TryBuildSuccessEvent(entityUid, ref message, channel, ref asName, out var msgOut))
            return;

        RaiseNetworkEvent(msgOut, allowList);

        StashRadioMessage(entityUid, message, channel, asName, msgOut);
    }

    /// <summary>
    /// Send a radio message to a channel.
    /// </summary>
    /// <param name="entityUid">The entity who is chatting</param>
    /// <param name="message">The message to send. This will be mutated with accents, to remove tags, etc.</param>
    /// <param name="channel">The channel the message can be heard in</param>
    /// <param name="asName">Override the name this entity will appear as.</param>
    public void SendRadioMessage(EntityUid entityUid, string message, RadioChannelPrototype channel, string asName = "")
    {
        if (!TryBuildSuccessEvent(entityUid, ref message, channel, ref asName, out var msgOut))
            return;

        TransmitToReceivers(entityUid, message, channel, asName, msgOut);
    }

    private void TransmitToReceivers(EntityUid entityUid, string message, RadioChannelPrototype channel, string asName,
        RadioSuccessEvent msgOut)
    {
        foreach (var receiver in GetRadioReceivers(entityUid, channel))
        {
            RaiseLocalEvent(receiver, msgOut);
        }

        StashRadioMessage(entityUid, message, channel, asName, msgOut);
    }

    private void StashRadioMessage(EntityUid entityUid, string message, RadioChannelPrototype channel, string asName,
        RadioSuccessEvent msgOut)
    {
        _replay.RecordServerMessage(msgOut);
        _adminLogger.Add(LogType.Chat, LogImpact.Low,
            $"Radio from {ToPrettyString(entityUid):user} on {channel} as {asName}: {message}");
    }

    private bool TryBuildSuccessEvent(EntityUid entityUid, ref string message, RadioChannelPrototype channel, ref string asName, [NotNullWhen(true)] out RadioSuccessEvent? msgOut, EntityUid? device = null)
    {
        message = SanitizeSpeechMessage(
            entityUid,
            message,
            out var emoteStr
        );

        // If you lol on the radio, you should lol in the emote chat.
        if (emoteStr?.Length > 0)
        {
            TrySendEmoteMessageWithoutRecursion(entityUid, emoteStr, asName);
        }

        if (string.IsNullOrEmpty(message))
        {
            msgOut = null;

            return false;
        }

        // Mitigation for exceptions such as https://github.com/space-wizards/space-station-14/issues/24671
        try
        {
            message = FormattedMessage.RemoveMarkup(message);
        }
        catch (Exception e)
        {
            _logger.GetSawmill("chat")
                .Error(
                    $"UID {entityUid} attempted to send {message} {(asName.Length > 0 ? "as name, " : "")} but threw a parsing error: {e}");

            msgOut = null;

            return false;
        }

        message = TransformSpeech(entityUid, FormattedMessage.RemoveMarkup(message));

        if (string.IsNullOrEmpty(message))
        {
            msgOut = null;

            return false;
        }

        if (string.IsNullOrEmpty(asName))
        {
            asName = GetSpeakerName(entityUid);
        }

        var verb = GetSpeechVerb(entityUid, message);

        if (TryComp<VoiceMaskComponent>(entityUid, out var mask))
        {
            asName = mask.VoiceName;

            if (mask.SpeechVerb != null && _proto.TryIndex<SpeechVerbPrototype>(mask.SpeechVerb, out var proto))
            {
                verb = proto;
            }
        }

        var name = SanitizeName(asName, UseEnglishGrammar);

        msgOut = new RadioSuccessEvent(
            entityUid,
            name,
            message,
            channel.ID,
            Loc.GetString(_random.Pick(verb.SpeechVerbStrings)),
            verb.FontId,
            verb.FontSize,
            verb.Bold,
            device: device
        );

        return true;
    }

    /// <summary>
    /// Get all the receivers for this message. This is not the network recipients!
    ///
    /// Radios work off of being a sender, and other devices being listeners.
    /// Both sides need to match up for a radio to be a valid receiver.
    ///
    /// Radios are also inherently server-side; the communication of a radio speaking into a player's ear is client-facing.
    /// </summary>
    private HashSet<EntityUid> GetRadioReceivers(EntityUid source, RadioChannelPrototype channel)
    {
        var recipients = new HashSet<EntityUid>();

        // Some systems like EMPs and jammers can block radios.
        var sendAttemptEv = new RadioSendAttemptEvent(channel, source);
        RaiseLocalEvent(ref sendAttemptEv);
        RaiseLocalEvent(source, ref sendAttemptEv);

        if (sendAttemptEv.Cancelled)
        {
            return recipients;
        }

        // Radios are sometimes map-scoped...
        var sourceMapId = Transform(source).MapID;
        // And if they're map-scoped they're usually tied to a breakable transmission server...
        var hasActiveServer = HasActiveServer(sourceMapId, channel.ID);

        var headsets = EntityQueryEnumerator<HeadsetComponent, TransformComponent>();
        while (headsets.MoveNext(out var receiver, out var headset, out var transform))
        {
            // Headsets are (currently) always short-range.
            if (IsValidRecipient(source, sourceMapId, channel, hasActiveServer, true, receiver, headset.ChannelNames, transform, false))
                recipients.Add(receiver);
        }

        var internals = EntityQueryEnumerator<InternalRadioComponent, TransformComponent>();
        while (internals.MoveNext(out var receiver, out var radio, out var transform))
        {
            // Internal radios get their abilities through a myriad of means, from the mechanical to magical,
            if (IsValidRecipient(source, sourceMapId, channel, hasActiveServer, !radio.IsInfiniteRange, receiver, radio.ReceiveChannels, transform, radio.CanListenOnAllChannels))
                recipients.Add(receiver);
        }

        var speakers = EntityQueryEnumerator<RadioSpeakerComponent, TransformComponent>();
        while (speakers.MoveNext(out var receiver, out var speaker, out var transform))
        {
            // Radio speakers have infinite range. Their trade-off is usually that they require power.
            if (IsValidRecipient(source, sourceMapId, channel, hasActiveServer, false, receiver, speaker.Channels, transform, false))
                recipients.Add(receiver);
        }

        return recipients;
    }

    private bool IsValidRecipient(
        EntityUid source,
        MapId sourceMapId,
        RadioChannelPrototype senderChannel,
        bool channelHasServer,
        bool isSourceShortRange,
        EntityUid receiver,
        HashSet<string> receiverChannels,
        TransformComponent? recipientTransform,
        bool canListenOnAllChannels
    )
    {
        if (!canListenOnAllChannels)
        {
            // Receivers must be open to receive the channel.
            if (!receiverChannels.Contains(senderChannel.ID))
                return false;
        }

        // Radio microphones ignore range limitations, but usually need power to work.
        if (!senderChannel.LongRange && !TryComp<RadioMicrophoneComponent>(source, out _))
        {
            // Short-range radio needs a relay server.
            if (!channelHasServer)
            {
                return false;
            }

            // Short-range radio must be received on the same map as the sender.
            if (isSourceShortRange && recipientTransform?.MapID != sourceMapId)
            {
                return false;
            }
        }

        // check if message can be sent to specific receiver
        var attemptEv = new RadioReceiveAttemptEvent(senderChannel, source, receiver);

        RaiseLocalEvent(ref attemptEv);
        RaiseLocalEvent(receiver, ref attemptEv);

        return !attemptEv.Cancelled;
    }

    private bool HasActiveServer(MapId mapId, string channelId)
    {
        var servers = EntityQuery<TelecomServerComponent, EncryptionKeyHolderComponent, ApcPowerReceiverComponent, TransformComponent>();
        foreach (var (_, keys, power, transform) in servers)
        {
            if (transform.MapID == mapId &&
                power.Powered &&
                keys.Channels.Contains(channelId))
            {
                return true;
            }
        }
        return false;
    }
}

/// <summary>
/// Raised when a character speaks on the radio.
/// </summary>
[Serializable]
public sealed class RadioSuccessEvent : EntityEventArgs
{
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

    public RadioSuccessEvent(
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
}
