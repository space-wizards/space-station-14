using System.Diagnostics.CodeAnalysis;
using Content.Server.Power.Components;
using Content.Server.Radio;
using Content.Server.Radio.Components;
using Content.Shared.Chat.V2.Components;
using Content.Shared.Database;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using JetBrains.FormatRipper.Elf;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.Chat.V2;

public sealed partial class ChatSystem
{
    public void SendRadioMessageViaDevice(EntityUid entityUid, string message, RadioChannelPrototype channel, EntityUid device, string asName = "", uint id = 0)
    {
        if (!TryBuildEvent(entityUid, ref message, channel, ref asName, out var msgOut, id, device))
            return;

        TransmitToReceivers(entityUid, message, channel, asName, msgOut);
    }

    public void SendRadioMessageWithSpeech(RadioCreatedEvent ev)
    {
        SendRadioMessageWithSpeech(ev.Speaker, ev.Message, ev.Channel, id: ev.Id);
    }

    /// <summary>
    /// Send a radio message via a channel, whispering (or shouting!) if needed.
    /// </summary>
    /// <param name="entityUid">The entity who is chatting</param>
    /// <param name="message">The message to send. This will be mutated with accents, to remove tags, etc.</param>
    /// <param name="channel">The channel the message can be heard in</param>
    /// <param name="asName">Override the name this entity will appear as.</param>
    /// <param name="id">The ID of the message. Defaults to zero, signifying an automated message.</param>
    public void SendRadioMessageWithSpeech(EntityUid entityUid, string message, RadioChannelPrototype channel, string asName = "", uint id = 0)
    {
        // If you don't have an internal radio, you need to send a message using your voice box.
        if (!TryComp<InternalRadioComponent>(entityUid, out var comp) || !comp.SendChannels.Contains(channel.ID))
        {
            TrySendWhisperMessage(entityUid, message, asName, id);
        }

        SendRadioMessage(entityUid, message, channel, asName, id);
    }

    /// <summary>
    /// Send a radio message via a channel to specific targets.
    /// </summary>
    /// <param name="entityUid">The entity who is chatting</param>
    /// <param name="message">The message to send. This will be mutated with accents, to remove tags, etc.</param>
    /// <param name="channel">The channel the message can be heard in</param>
    /// <param name="asName">Override the name this entity will appear as.</param>
    /// <param name="id">The ID of the message. Defaults to zero, signifying an automated message.</param>
    public void SendRadioMessageToTargets(EntityUid entityUid, string message, RadioChannelPrototype channel, Filter allowList, string asName = "", uint id = 0)
    {
        if (!TryBuildEvent(entityUid, ref message, channel, ref asName, out var msgOut, id))
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
    /// <param name="id">The ID of the message. Defaults to zero, signifying an automated message.</param>
    public void SendRadioMessage(EntityUid entityUid, string message, RadioChannelPrototype channel, string asName = "", uint id = 0)
    {
        if (!TryBuildEvent(entityUid, ref message, channel, ref asName, out var msgOut, id))
            return;

        TransmitToReceivers(entityUid, message, channel, asName, msgOut);
    }

    private void TransmitToReceivers(EntityUid entityUid, string message, RadioChannelPrototype channel, string asName,
        RadioEmittedEvent msgOut)
    {
        foreach (var receiver in GetRadioReceivers(entityUid, channel))
        {
            RaiseLocalEvent(receiver, msgOut);
        }

        StashRadioMessage(entityUid, message, channel, asName, msgOut);
    }

    private void StashRadioMessage(EntityUid entityUid, string message, RadioChannelPrototype channel, string asName, RadioEmittedEvent msgOut)
    {
        _replay.RecordServerMessage(msgOut);
        LogMessage(entityUid, "radio", msgOut.Id, channel.ID, asName, message);
    }

    private bool TryBuildEvent(EntityUid entityUid, ref string message, RadioChannelPrototype channel, ref string asName, [NotNullWhen(true)] out RadioEmittedEvent? msgOut, uint id, EntityUid? device = null)
    {
        if (!TrySanitizeAndTransformSpokenMessage(entityUid, ref message, ref asName, out var name))
        {
            msgOut = null;

            return false;
        }

        msgOut = new RadioEmittedEvent(
            entityUid,
            name,
            message,
            channel.ID,
            device: device,
            id
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
