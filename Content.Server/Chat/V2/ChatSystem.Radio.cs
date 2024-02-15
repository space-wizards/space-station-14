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
    public void InitializeRadio()
    {
        SubscribeNetworkEvent<HeadsetRadioAttemptedEvent>((msg, args) => { HandleAttemptRadioMessage(args.SenderSession, msg.Speaker, msg.Message, msg.Channel, false); });
        SubscribeNetworkEvent<InternalRadioAttemptedEvent>((msg, args) => { HandleAttemptRadioMessage(args.SenderSession, msg.Speaker, msg.Message, msg.Channel, true); });
    }

    private void HandleAttemptRadioMessage(ICommonSession player, NetEntity entity, string message, string channel, bool isInnate)
    {
        var entityUid = GetEntity(entity);

        if (player.AttachedEntity != entityUid)
        {
            // Nice try bozo.
            return;
        }

        // Are they rate-limited
        if (IsRateLimited(entityUid, out var reason))
        {
            RaiseNetworkEvent(new RadioAttemptFailedEvent(entity, reason), player);

            return;
        }

        HashSet<string> channels;

        // Sanity check: if you can't chat you shouldn't be chatting.
        if (isInnate)
        {
            if (!TryComp<InternalRadioComponent>(entityUid, out var comp))
            {
                RaiseNetworkEvent(new RadioAttemptFailedEvent(entity, "You can't talk on any radio channel."), player);

                return;
            }

            channels = comp.SendChannels;
        }
        else
        {
            if (!TryComp<HeadsetRadioableComponent>(entityUid, out var comp))
            {
                RaiseNetworkEvent(new RadioAttemptFailedEvent(entity, "You can't talk on any radio channel."), player);

                return;
            }

            channels = comp.Channels;
        }

        // Using LINQ here, pls don't murder me PJB 🙏
        if (!channels.Contains(channel))
        {
            // TODO: Add locstring
            RaiseNetworkEvent(new RadioAttemptFailedEvent(entity, $"You can't talk on the {channel} radio channel."), player);

            return;
        }

        if (!_proto.TryIndex(channel, out RadioChannelPrototype? radioChannelProto))
        {
            // TODO: Add locstring
            RaiseNetworkEvent(new RadioAttemptFailedEvent(entity, $"The {channel} radio channel doesn't exist!"), player);

            return;
        }

        // Is the message too long?
        if (message.Length > MaxChatMessageLength)
        {
            RaiseNetworkEvent(
                new RadioAttemptFailedEvent(
                    entity,
                    Loc.GetString("chat-manager-max-message-length-exceeded-message", ("limit", MaxChatMessageLength))
                    ),
                player);

            return;
        }

        // All good; let's actually send a chat message.
        SendRadioMessage(entityUid, message, radioChannelProto, whisper: true);
    }

    /// <summary>
    /// Send a chat in Local.
    /// </summary>
    /// <param name="entityUid">The entity who is chatting</param>
    /// <param name="message">The message to send. This will be mutated with accents, to remove tags, etc.</param>
    /// <param name="channel">The channel the message can be heard in</param>
    /// <param name="asName">Override the name this entity will appear as.</param>
    /// <param name="allowList">Override the normal selection of listeners with a specific filter. Useful for off-map activities like salvaging.</param>
    /// <param name="whisper">If this radio message should be whispered out.</param>
    /// <param name="device">The entity of the RadioMicrophone that sent this message, if applicable.</param>
    public void SendRadioMessage(EntityUid entityUid, string message, RadioChannelPrototype channel, string asName = "", Filter? allowList = null, bool whisper = false, EntityUid? device = null)
    {
        message = SanitizeInCharacterMessage(
            entityUid,
            message,
            out var emoteStr
        );

        // If you lol on the radio, you should lol in the emote chat.
        if (emoteStr?.Length > 0)
        {
            TrySendEmoteMessage(entityUid, emoteStr, asName, true);
        }

        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        // If you don't have intrinsic radio, you need to whisper to send a message using your voice box.
        if (!TryComp<InternalRadioComponent>(entityUid, out var comp) || !comp.SendChannels.Contains(channel.ID))
        {
            if (whisper)
            {
                TrySendWhisperMessage(entityUid, message, asName);
            }
        }

        // Mitigation for exceptions such as https://github.com/space-wizards/space-station-14/issues/24671
        try
        {
            message = FormattedMessage.RemoveMarkup(message);
        }
        catch (Exception e)
        {
            _logger.GetSawmill("chat").Error($"UID {entityUid} attempted to send {message} {(asName.Length > 0 ? "as name, " : "")} but threw a parsing error: {e}");

            return;
        }

        message = TransformSpeech(entityUid, FormattedMessage.RemoveMarkup(message));

        if (string.IsNullOrEmpty(message))
        {
            return;
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

        var name = FormattedMessage.EscapeText(asName);

        var msgOut = new EntityRadioLocalEvent(
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

        if (allowList != null)
        {
            RaiseNetworkEvent(msgOut, allowList);
        }
        else
        {
            // Now fire it off to receivers locally. They'll handle shipping it back to their owning client if needed.
            foreach (var receiver in GetRadioReceivers(entityUid, channel))
            {
                RaiseLocalEvent(receiver, msgOut);
            }
        }

        // And finally, stash it in the replay and log.
        _replay.RecordServerMessage(msgOut);
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Radio from {ToPrettyString(entityUid):user} on {channel} as {asName}: {message}");
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
