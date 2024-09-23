using Content.Server.Chat.Systems;
using Content.Server.Interaction;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Radio;
using Content.Server.Radio.Components;
using Content.Server.Speech;
using Content.Server.VoiceMask;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Radio.Components;
using Content.Shared.Radio;
using Content.Shared.Speech;
using Content.Shared.Telephone;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Server.Administration.Logs;
using Robust.Shared.Replays;

namespace Content.Server.Telephone;

public sealed class TelephoneSystem : SharedTelephoneSystem
{
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IReplayRecordingManager _replay = default!;

    // set used to prevent radio feedback loops.
    private readonly HashSet<string> _messages = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TelephoneComponent, TelephoneCallAttemptEvent>(OnIncomingCallAttempt);
        SubscribeLocalEvent<TelephoneComponent, ComponentShutdown>(OnComponentShutdown);

        //SubscribeLocalEvent<TelephoneComponent, ListenEvent>(OnListen);
    }

    #region: Events

    private void OnIncomingCallAttempt(EntityUid uid, TelephoneComponent component, ref TelephoneCallAttemptEvent ev)
    {
        if (!IsTelephoneReachable(uid, component) || IsTelephoneEngaged(uid, component))
        {
            ev.Cancelled = true;
            return;
        }
    }

    private void OnComponentShutdown(EntityUid uid, TelephoneComponent component, ref ComponentShutdown ev)
    {
        TerminateTelephoneCall(uid, component);
    }

    private void OnAttemptListen(EntityUid uid, RadioMicrophoneComponent component, ListenAttemptEvent args)
    {
        if (component.PowerRequired && !this.IsPowered(uid, EntityManager)
            || component.UnobstructedRequired && !_interaction.InRangeUnobstructed(args.Source, uid, 0))
        {
            args.Cancel();
        }
    }

    private void OnListen(EntityUid uid, TelephoneComponent component, ListenEvent args)
    {
        if (args.Source == uid)
            return;

        //if (_recentlySent.Add((args.Message, args.Source)))
        //    _radio.SendRadioMessage(args.Source, args.Message, _protoMan.Index<RadioChannelPrototype>(component.BroadcastChannel), uid);
    }

    private void OnReceiveRadio(EntityUid uid, RadioSpeakerComponent component, ref RadioReceiveEvent args)
    {
        if (uid == args.RadioSource)
            return;

        var nameEv = new TransformSpeakerNameEvent(args.MessageSource, Name(args.MessageSource));
        RaiseLocalEvent(args.MessageSource, nameEv);

        var name = Loc.GetString("speech-name-relay",
            ("speaker", Name(uid)),
            ("originalName", nameEv.Name));

        // log to chat so people can identity the speaker/source, but avoid clogging ghost chat if there are many radios
        _chat.TrySendInGameICMessage(uid, args.Message, InGameICChatType.Whisper, ChatTransmitRange.GhostRangeLimit, nameOverride: name, checkRadioPrefix: false);
    }

    #endregion

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityManager.EntityQueryEnumerator<TelephoneComponent>();
        while (query.MoveNext(out var ent, out var entTelephone))
        {
            if (!IsTelephoneReachable(ent, entTelephone) && IsTelephoneEngaged(ent, entTelephone))
            {
                TerminateTelephoneCall(ent, entTelephone);
                continue;
            }

            switch (entTelephone.CurrentState)
            {
                case TelephoneState.Ringing:
                    if (_timing.RealTime > entTelephone.StateStartTime + TimeSpan.FromSeconds(entTelephone.RingingTimeout))
                        HangUpTelephone(ent, entTelephone);

                    else if (entTelephone.RingTone != null &&
                        _timing.RealTime > entTelephone.NextToneTime)
                    {
                        _audio.PlayPvs(entTelephone.RingTone, ent);
                        entTelephone.NextToneTime = _timing.RealTime + TimeSpan.FromSeconds(entTelephone.RingInterval);
                    }

                    break;

                case TelephoneState.HangingUp:
                    if (_timing.RealTime > entTelephone.StateStartTime + TimeSpan.FromSeconds(entTelephone.HangingUpTimeout))
                        TerminateTelephoneCall(ent, entTelephone);

                    break;
            }
        }
    }

    public void CallTelephone(EntityUid uid, TelephoneComponent component, EntityUid source, EntityUid? user = null)
    {
        if (IsTelephoneEngaged(uid, component))
            return;

        if (!TryComp<TelephoneComponent>(source, out var sourceTelephone))
            return;

        var evCallAttempt = new TelephoneCallAttemptEvent(source, uid, user);
        RaiseLocalEvent(uid, ref evCallAttempt);
        RaiseLocalEvent(source, ref evCallAttempt);

        if (evCallAttempt.Cancelled)
        {
            HangUpTelephone(source, sourceTelephone);
            return;
        }

        component.User = null;
        component.LinkedTelephone = source;
        SetTelephoneState(uid, component, TelephoneState.Ringing);

        sourceTelephone.User = user;
        sourceTelephone.LinkedTelephone = uid;
        SetTelephoneState(source, sourceTelephone, TelephoneState.Calling);

        var evCall = new TelephoneCallEvent(source, uid, user);
        RaiseLocalEvent(uid, ref evCall);
        RaiseLocalEvent(source, ref evCall);
    }

    public void AnswerTelephone(EntityUid uid, TelephoneComponent component, EntityUid? user = null)
    {
        if (component.CurrentState != TelephoneState.Ringing)
            return;

        if (!TryComp<TelephoneComponent>(component.LinkedTelephone, out var sourceTelephone))
            return;

        component.User = user;

        SetTelephoneState(uid, component, TelephoneState.InCall);
        SetTelephoneState(component.LinkedTelephone.Value, sourceTelephone, TelephoneState.InCall);

        var evCallCommenced = new TelephoneCallCommencedEvent(component.LinkedTelephone.Value, uid);
        RaiseLocalEvent(uid, ref evCallCommenced);
    }

    public void HangUpTelephone(EntityUid uid, TelephoneComponent component)
    {
        if (component.CurrentState == TelephoneState.HangingUp)
            return;

        var evHungUp = new TelephoneHungUpEvent(uid);

        if (TryComp<TelephoneComponent>(component.LinkedTelephone, out var linkedTelephone))
        {
            linkedTelephone.User = null;
            linkedTelephone.LinkedTelephone = null;
            SetTelephoneState(component.LinkedTelephone.Value, linkedTelephone, TelephoneState.HangingUp);

            RaiseLocalEvent(component.LinkedTelephone.Value, ref evHungUp);
        }

        component.User = null;
        component.LinkedTelephone = null;
        SetTelephoneState(uid, component, TelephoneState.HangingUp);

        RaiseLocalEvent(uid, ref evHungUp);
    }

    public void TerminateTelephoneCall(EntityUid uid, TelephoneComponent component)
    {
        if (!IsTelephoneEngaged(uid, component))
            return;

        var evCallTerminated = new TelephoneCallTerminatedEvent();

        if (TryComp<TelephoneComponent>(component.LinkedTelephone, out var linkedTelephone))
        {
            linkedTelephone.User = null;
            linkedTelephone.LinkedTelephone = null;
            SetTelephoneState(component.LinkedTelephone.Value, linkedTelephone, TelephoneState.Idle);

            RaiseLocalEvent(component.LinkedTelephone.Value, ref evCallTerminated);
        }

        component.User = null;
        component.LinkedTelephone = null;
        SetTelephoneState(uid, component, TelephoneState.Idle);

        RaiseLocalEvent(uid, ref evCallTerminated);
    }

    public void SendTelephoneMessage(EntityUid messageSource, string message, RadioChannelPrototype channel, EntityUid radioSource, bool escapeMarkup = true)
    {
        // TODO if radios ever garble / modify messages, feedback-prevention needs to be handled better than this.
        if (!_messages.Add(message))
            return;

        var name = TryComp(messageSource, out VoiceMaskComponent? mask) && mask.Enabled
            ? mask.VoiceName
            : MetaData(messageSource).EntityName;

        name = FormattedMessage.EscapeText(name);

        SpeechVerbPrototype speech;
        if (mask != null
            && mask.Enabled
            && mask.SpeechVerb != null
            && _prototype.TryIndex(mask.SpeechVerb, out var proto))
        {
            speech = proto;
        }

        else
        {
            speech = _chat.GetSpeechVerb(messageSource, message);
        }

        var content = escapeMarkup
            ? FormattedMessage.EscapeText(message)
            : message;

        var wrappedMessage = Loc.GetString(speech.Bold ? "chat-radio-message-wrap-bold" : "chat-radio-message-wrap",
            ("color", channel.Color),
            ("fontType", speech.FontId),
            ("fontSize", speech.FontSize),
            ("verb", Loc.GetString(_random.Pick(speech.SpeechVerbStrings))),
            ("channel", $"\\[{channel.LocalizedName}\\]"),
            ("name", name),
            ("message", content));

        // most radios are relayed to chat, so lets parse the chat message beforehand
        var chat = new ChatMessage(
            ChatChannel.Radio,
            message,
            wrappedMessage,
            NetEntity.Invalid,
            null);

        var chatMsg = new MsgChatMessage { Message = chat };
        var ev = new RadioReceiveEvent(message, messageSource, channel, radioSource, chatMsg);

        var sendAttemptEv = new RadioSendAttemptEvent(channel, radioSource);
        RaiseLocalEvent(ref sendAttemptEv);
        RaiseLocalEvent(radioSource, ref sendAttemptEv);
        var canSend = !sendAttemptEv.Cancelled;

        var sourceMapId = Transform(radioSource).MapID;


        if (!channel.LongRange && transform.MapID != sourceMapId && !radio.GlobalReceive)
            return;

 

        // check if message can be sent to specific receiver
        var attemptEv = new RadioReceiveAttemptEvent(channel, radioSource, receiver);
        RaiseLocalEvent(ref attemptEv);
        RaiseLocalEvent(receiver, ref attemptEv);

        if (attemptEv.Cancelled)
            return;

        // send the message
        RaiseLocalEvent(receiver, ref ev);

        if (name != Name(messageSource))
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Telephone message from {ToPrettyString(messageSource):user} as {name} on {channel.LocalizedName}: {message}");
        else
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Telephone message from {ToPrettyString(messageSource):user} on {channel.LocalizedName}: {message}");

        _replay.RecordServerMessage(chat);
        _messages.Remove(message);
    }

    private void SetTelephoneState(EntityUid uid, TelephoneComponent component, TelephoneState newState)
    {
        component.CurrentState = newState;
        component.StateStartTime = _timing.RealTime;

        _appearanceSystem.SetData(uid, TelephoneVisuals.Key, component.CurrentState);
    }

    public bool IsTelephoneReachable(EntityUid uid, TelephoneComponent component)
    {
        if (TryComp<ApcPowerReceiverComponent>(uid, out var apcPowerReceiver) && !apcPowerReceiver.Powered)
            return false;

        return true;
    }

    public bool IsTelephoneEngaged(EntityUid uid, TelephoneComponent component)
    {
        return component.CurrentState != TelephoneState.Idle;
    }
}
