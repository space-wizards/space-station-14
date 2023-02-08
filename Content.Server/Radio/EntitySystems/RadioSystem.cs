using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Server.Radio.Components;
using Content.Server.VoiceMask;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Radio;
using Robust.Server.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Replays;
using Robust.Shared.Utility;

namespace Content.Server.Radio.EntitySystems;

/// <summary>
///     This system handles intrinsic radios and the general process of converting radio messages into chat messages.
/// </summary>
public sealed class RadioSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly IReplayRecordingManager _replay = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    // set used to prevent radio feedback loops.
    private readonly HashSet<string> _messages = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IntrinsicRadioReceiverComponent, RadioReceiveEvent>(OnIntrinsicReceive);
        SubscribeLocalEvent<IntrinsicRadioTransmitterComponent, EntitySpokeEvent>(OnIntrinsicSpeak);
    }

    private void OnIntrinsicSpeak(EntityUid uid, IntrinsicRadioTransmitterComponent component, EntitySpokeEvent args)
    {
        if (args.Channel != null && component.Channels.Contains(args.Channel.ID))
        {
            SendRadioMessage(uid, args.Message, args.Channel);
            args.Channel = null; // prevent duplicate messages from other listeners.
        }
    }

    private void OnIntrinsicReceive(EntityUid uid, IntrinsicRadioReceiverComponent component, RadioReceiveEvent args)
    {
        if (TryComp(uid, out ActorComponent? actor))
            _netMan.ServerSendMessage(args.ChatMsg, actor.PlayerSession.ConnectedClient);
    }

    public void SendRadioMessage(EntityUid source, string message, RadioChannelPrototype channel)
    {
        // TODO if radios ever garble / modify messages, feedback-prevention needs to be handled better than this.
        if (!_messages.Add(message))
            return;

        var name = TryComp(source, out VoiceMaskComponent? mask) && mask.Enabled
            ? mask.VoiceName
            : MetaData(source).EntityName;

        name = FormattedMessage.EscapeText(name);

        // most radios are relayed to chat, so lets parse the chat message beforehand
        var chat = new ChatMessage(
            ChatChannel.Radio,
            message,
            Loc.GetString("chat-radio-message-wrap", ("color", channel.Color), ("channel", $"\\[{channel.LocalizedName}\\]"), ("name", name), ("message", FormattedMessage.EscapeText(message))),
            EntityUid.Invalid);
        var chatMsg = new MsgChatMessage { Message = chat };

        var ev = new RadioReceiveEvent(message, source, channel, chatMsg);
        var attemptEv = new RadioReceiveAttemptEvent(message, source, channel);

        foreach (var radio in EntityQuery<ActiveRadioComponent>())
        {
            // TODO map/station/range checks?

            if (!radio.Channels.Contains(channel.ID))
                continue;

            RaiseLocalEvent(radio.Owner, attemptEv);
            if (attemptEv.Cancelled)
            {
                attemptEv.Uncancel();
                continue;
            }

            RaiseLocalEvent(radio.Owner, ev);
        }

        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Radio message from {ToPrettyString(source):sender} on {channel.LocalizedName}: {message}");

        _replay.QueueReplayMessage(chat);
        _messages.Remove(message);
    }
}
