using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Server.Power.Components;
using Content.Shared.Radio.Components;
using Content.Server.Starlight.TTS;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.PDA;
using Content.Shared.Radio;
using Content.Shared.Speech;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Replays;
using Robust.Shared.Utility;
using Content.Server._Starlight.Radio.Systems;
// Starlight - Start
using Content.Shared._Starlight.Language;
using Content.Server._Starlight.Language;
using Content.Shared._Starlight.Silicons.Borgs;
// Starlight - End

namespace Content.Server.Radio.EntitySystems;

/// <summary>
///     This system handles intrinsic radios and the general process of converting radio messages into chat messages.
/// </summary>
public sealed class RadioSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly IReplayRecordingManager _replay = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly RadioChimeSystem _chime = default!; //🌟Starlight🌟
    [Dependency] private readonly LanguageSystem _language = default!; // Starlight

    // set used to prevent radio feedback loops.
    private readonly HashSet<string> _messages = new();

    private EntityQuery<TelecomExemptComponent> _exemptQuery;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IntrinsicRadioReceiverComponent, RadioReceiveEvent>(OnIntrinsicReceive);
        SubscribeLocalEvent<IntrinsicRadioTransmitterComponent, EntitySpokeEvent>(OnIntrinsicSpeak);

        _exemptQuery = GetEntityQuery<TelecomExemptComponent>();
    }

    private void OnIntrinsicSpeak(EntityUid uid, IntrinsicRadioTransmitterComponent component, EntitySpokeEvent args)
    {
        if (args.Channel != null && component.Channels.Contains(args.Channel.ID))
        {
            SendRadioMessage(uid, args.Message, args.Channel, uid, args.Language); // Starlight
            args.Channel = null; // prevent duplicate messages from other listeners.
        }
    }

    private void OnIntrinsicReceive(EntityUid uid, IntrinsicRadioReceiverComponent component, ref RadioReceiveEvent args)
    {
        if (TryComp(uid, out ActorComponent? actor))
        {
            // Starlight - Start
            var listener = component.Owner;
            var msg = args.OriginalChatMsg;

            if (listener != null && !_language.CanUnderstand(listener, args.Language.ID))
                msg = args.LanguageObfuscatedChatMsg;

            _netMan.ServerSendMessage(new MsgChatMessage { Message = msg }, actor.PlayerSession.Channel);
            // Starlight - End
        }
    }

    /// <summary>
    /// Send radio message to all active radio listeners
    /// </summary>
    public void SendRadioMessage(
        EntityUid messageSource,
        string message,
        ProtoId<RadioChannelPrototype> channel,
        EntityUid radioSource,
        LanguagePrototype? language = null, // Starlight
        bool escapeMarkup = true)
    {
        SendRadioMessage(messageSource, message, _prototype.Index(channel), radioSource, escapeMarkup: escapeMarkup, language: language); // Starlight
    }

    /// <summary>
    /// Send radio message to all active radio listeners
    /// </summary>
    /// <param name="messageSource">Entity that spoke the message</param>
    /// <param name="radioSource">Entity that picked up the message and will send it, e.g. headset</param>
    public void SendRadioMessage(
        EntityUid messageSource,
        string message,
        RadioChannelPrototype channel,
        EntityUid radioSource,
        LanguagePrototype? language = null, // Starlight
        bool escapeMarkup = true)
    {
        // Starlight - start
        if (language == null)
            language = _language.GetLanguage(messageSource);

        if (!language.SpeechOverride.AllowRadio)
            return;
        // Starlight - End

        // TODO if radios ever garble / modify messages, feedback-prevention needs to be handled better than this.
        if (!_messages.Add(message))
            return;

        var meta = MetaData(messageSource);
        var entityName = meta?.EntityName ?? string.Empty;
        var evt = new TransformSpeakerNameEvent(messageSource, entityName);
        RaiseLocalEvent(messageSource, evt);

        var name = evt.VoiceName;
        if (string.IsNullOrEmpty(name))
            name = entityName;
        if (name == null)
            name = string.Empty;
        name = FormattedMessage.EscapeText(name);

        SpeechVerbPrototype speech;
        if (evt.SpeechVerb != null && _prototype.Resolve(evt.SpeechVerb, out var evntProto))
            speech = evntProto;
        else
            speech = _chat.GetSpeechVerb(messageSource, message);

        var content = escapeMarkup
            ? FormattedMessage.EscapeText(message)
            : message;

        _chime.TryGetSenderHeadsetChime(messageSource, out var chime);

        var wrappedMessage = WrapRadioMessage(messageSource, channel, name, content, language, false);

        // most radios are relayed to chat, so lets parse the chat message beforehand

        var msg = new ChatMessage(ChatChannel.Radio, content, wrappedMessage, NetEntity.Invalid, null); // Starlight

        var obfuscated = _language.ObfuscateSpeech(content, language);
        var obfuscatedWrapped = WrapRadioMessage(messageSource, channel, name, obfuscated, language, true);
        var notUdsMsg = new ChatMessage(ChatChannel.Radio, obfuscated, obfuscatedWrapped, NetEntity.Invalid, null) { Chime = chime, };
        var ev = new RadioReceiveEvent(messageSource, channel, msg, notUdsMsg, language, radioSource, []);
        // Starlight - End

        var sendAttemptEv = new RadioSendAttemptEvent(channel, radioSource);
        RaiseLocalEvent(ref sendAttemptEv);
        RaiseLocalEvent(radioSource, ref sendAttemptEv);
        var canSend = !sendAttemptEv.Cancelled;

        var sourceMapId = Transform(radioSource).MapID;
        var hasActiveServer = HasActiveServer(sourceMapId, channel.ID);
        var sourceServerExempt = _exemptQuery.HasComp(radioSource);

        var radioQuery = EntityQueryEnumerator<ActiveRadioComponent, TransformComponent>();
        while (canSend && radioQuery.MoveNext(out var receiver, out var radio, out var transform))
        {
            if (!radio.ReceiveAllChannels)
            {
                if (!radio.Channels.Contains(channel.ID) || (TryComp<IntercomComponent>(receiver, out var intercom) &&
                                                             !intercom.SupportedChannels.Contains(channel.ID)))
                    continue;
            }

            if (!channel.LongRange && transform.MapID != sourceMapId && !radio.GlobalReceive)
                continue;

            // don't need telecom server for long range channels or handheld radios and intercoms
            var needServer = !channel.LongRange && !sourceServerExempt;
            if (needServer && !hasActiveServer)
                continue;

            // check if message can be sent to specific receiver
            var attemptEv = new RadioReceiveAttemptEvent(channel, radioSource, receiver);
            RaiseLocalEvent(ref attemptEv);
            RaiseLocalEvent(receiver, ref attemptEv);
            if (attemptEv.Cancelled)
                continue;

            // send the message
            RaiseLocalEvent(receiver, ref ev);

        }

        RaiseLocalEvent(new RadioSpokeEvent
        {
            Source = messageSource,
            Message = message,
            Language = language, // Starlight-edit: Languages
            Receivers = [.. ev.Receivers]
        });

        if (name != Name(messageSource))
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Radio message from {ToPrettyString(messageSource):user} as {name} on {channel.LocalizedName}: {message}");
        else
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Radio message from {ToPrettyString(messageSource):user} on {channel.LocalizedName}: {message}");

        _replay.RecordServerMessage(msg); // Starlight-edit: Languages
        _messages.Remove(message);
    }

    // Starlight - Start
    private (string, string) GetJobIcon(EntityUid messageSource)
    {
        var iconId = "JobIconNoId";
        var jobName = "";

        if (_accessReader.FindAccessItemsInventory(messageSource, out var items))
        {
            foreach (var item in items)
            {
                // ID Card
                if (TryComp<IdCardComponent>(item, out var id))
                {
                    iconId = id.JobIcon;
                    jobName = id.LocalizedJobTitle;
                    break;
                }

                // PDA
                if (TryComp<PdaComponent>(item, out var pda)
                    && pda.ContainedId != null
                    && TryComp(pda.ContainedId, out id))
                {
                    iconId = id.JobIcon;
                    jobName = id.LocalizedJobTitle;
                    break;
                }
            }
        }

        if (HasComp<BorgChassisComponent>(messageSource) || HasComp<BorgBrainComponent>(messageSource))
        {
            iconId = "JobIconBorg";
            jobName = Loc.GetString("job-name-borg");
        }

        if (HasComp<StationAiHeldComponent>(messageSource) || (TryComp<StationAIShuntComponent>(messageSource, out var aiShunt) && aiShunt.Return.HasValue))
        {
            iconId = "JobIconStationAi";
            jobName = Loc.GetString("job-name-station-ai");
        }

        jobName ??= "";

        return (iconId, jobName);
    }
    private string WrapRadioMessage(
        EntityUid source,
        RadioChannelPrototype channel,
        string name,
        string message,
        LanguagePrototype language,
        bool obfuscated
        )
    {
        // TODO: code duplication with ChatSystem.WrapMessage
        var speech = _chat.GetSpeechVerb(source, message);
        var languageColor = channel.Color;

        if (language.SpeechOverride.Color is { } colorOverride)
            languageColor = Color.InterpolateBetween(Color.White, colorOverride, colorOverride.A); // Changed first param to Color.White so it shows color correctly.

        var (iconId, jobName) = GetJobIcon(source);

        var namestring = $"[icon src=\"{iconId}\" tooltip=\"{jobName}\"] {name}";
        if (_language.GetLanguageIcon(language, obfuscated))
            namestring = $"[icon src=\"{iconId}\" tooltip=\"{jobName}\"] [icon src=\"{language.Icon}\" tooltip=\"{language.Name}\"] {name}";

        var fonttype = language.SpeechOverride.FontId ?? speech.FontId;
        if ((language.SpeechOverride.ObfuscationFont ?? false) && !obfuscated)
            fonttype = speech.FontId;

        return Loc.GetString(speech.Bold ? "chat-radio-message-wrap-bold" : "chat-radio-message-wrap",
                ("color", channel.Color),
                ("languageColor", languageColor),
                ("fontType", fonttype),
                ("fontSize", language.SpeechOverride.FontSize ?? speech.FontSize),
                ("verb", Loc.GetString(_random.Pick(speech.SpeechVerbStrings))),
                ("channel", $"\\[{channel.LocalizedName}\\]"),
                ("name", namestring),
                ("message", message));
    }
    // Starlight - End

    /// <inheritdoc cref="TelecomServerComponent"/>
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
