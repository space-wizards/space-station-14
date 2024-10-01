using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Server.Interaction;
using Content.Server.Power.EntitySystems;
using Content.Server.Speech;
using Content.Server.Speech.Components;
using Content.Server.VoiceMask;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Mind.Components;
using Content.Shared.Power;
using Content.Shared.Speech;
using Content.Shared.Telephone;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Replays;
using System.Linq;

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

    // Has set used to prevent telephone feedback loops
    private HashSet<(EntityUid, string, Entity<TelephoneComponent>)> _recentChatMessages = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TelephoneComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<TelephoneComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<TelephoneComponent, ListenAttemptEvent>(OnAttemptListen);
        SubscribeLocalEvent<TelephoneComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<TelephoneComponent, TelephoneMessageReceivedEvent>(OnTelephoneMessageReceived);
    }

    #region: Events

    private void OnPowerChanged(Entity<TelephoneComponent> telephone, ref PowerChangedEvent ev)
    {
        if (!ev.Powered)
            TerminateTelephoneCalls(telephone);
    }

    private void OnComponentShutdown(Entity<TelephoneComponent> telephone, ref ComponentShutdown ev)
    {
        TerminateTelephoneCalls(telephone);
    }

    private void OnAttemptListen(Entity<TelephoneComponent> telephone, ref ListenAttemptEvent args)
    {
        if (!this.IsPowered(telephone, EntityManager)
            || !_interaction.InRangeUnobstructed(args.Source, telephone.Owner, 0))
        {
            args.Cancel();
        }
    }

    private void OnListen(Entity<TelephoneComponent> telephone, ref ListenEvent args)
    {
        if (args.Source == telephone.Owner)
            return;

        // Ignore background chatter
        if (!HasComp<MindContainerComponent>(args.Source))
            return;

        if (_recentChatMessages.Add((args.Source, args.Message, telephone)))
            SendTelephoneMessage(args.Source, args.Message, telephone);
    }

    private void OnTelephoneMessageReceived(Entity<TelephoneComponent> telephone, ref TelephoneMessageReceivedEvent args)
    {
        if (telephone == args.TelephoneSource)
            return;

        if (!this.IsPowered(telephone, EntityManager))
            return;

        var nameEv = new TransformSpeakerNameEvent(args.MessageSource, Name(args.MessageSource));
        RaiseLocalEvent(args.MessageSource, nameEv);

        var name = Loc.GetString("speech-name-relay",
            ("speaker", Name(telephone)),
            ("originalName", nameEv.VoiceName));

        var volume = telephone.Comp.SpeakerVolume == TelephoneVolume.Speak ? InGameICChatType.Speak : InGameICChatType.Whisper;
        _chat.TrySendInGameICMessage(telephone, args.Message, volume, ChatTransmitRange.GhostRangeLimit, nameOverride: name, checkRadioPrefix: false);
    }

    #endregion

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityManager.EntityQueryEnumerator<TelephoneComponent>();
        while (query.MoveNext(out var ent, out var entTelephone))
        {
            var telephone = new Entity<TelephoneComponent>(ent, entTelephone);

            if (!this.IsPowered(telephone, EntityManager) && IsTelephoneEngaged(telephone))
            {
                TerminateTelephoneCalls(telephone);
                continue;
            }

            switch (entTelephone.CurrentState)
            {
                case TelephoneState.Ringing:
                    if (_timing.RealTime > entTelephone.StateStartTime + TimeSpan.FromSeconds(entTelephone.RingingTimeout))
                        EndTelephoneCalls(telephone);

                    else if (entTelephone.RingTone != null &&
                        _timing.RealTime > entTelephone.NextRingToneTime)
                    {
                        _audio.PlayPvs(entTelephone.RingTone, ent);
                        entTelephone.NextRingToneTime = _timing.RealTime + TimeSpan.FromSeconds(entTelephone.RingInterval);
                    }

                    break;

                case TelephoneState.EndingCall:
                    if (_timing.RealTime > entTelephone.StateStartTime + TimeSpan.FromSeconds(entTelephone.HangingUpTimeout))
                        TerminateTelephoneCalls(telephone);

                    break;
            }
        }

        _recentChatMessages.Clear();
    }

    public void BroadcastCallToTelephones(Entity<TelephoneComponent> source, HashSet<Entity<TelephoneComponent>> receivers, EntityUid user, bool isEmergency = false)
    {
        var options = new TelephoneCallOptions()
        {
            ForceConnect = true,
            MuteReceiver = isEmergency,
        };

        foreach (var receiver in receivers)
            CallTelephone(source, receiver, user, options);

        // If no connections could be made, time out the telephone
        if (!IsTelephoneEngaged(source))
            EndTelephoneCalls(source);
    }

    public void CallTelephone(Entity<TelephoneComponent> source, Entity<TelephoneComponent> receiver, EntityUid user, TelephoneCallOptions? options = null)
    {
        if (!IsSourceAbleToConnectToReceiver(source, receiver))
            return;

        // If a connection cannot be made, time out the telephone
        if ((IsTelephoneEngaged(receiver) || !this.IsPowered(receiver, EntityManager)) &&
            options?.ForceConnect == false && options?.ForceJoin == false)
        {
            EndTelephoneCalls(source);
            return;
        }

        var evCallAttempt = new TelephoneCallAttemptEvent(source, receiver, user);

        RaiseLocalEvent(source, ref evCallAttempt);
        RaiseLocalEvent(receiver, ref evCallAttempt);

        if (evCallAttempt.Cancelled)
        {
            // Force connected calls could originate from a broadcast to multiple telephones,
            // so this needs to be handled elsewhere
            if (options?.ForceConnect == false)
                EndTelephoneCalls(source);

            return;
        }

        source.Comp.LinkedTelephones.Add(receiver);
        source.Comp.Muted = options?.MuteSource == true;

        if (options?.ForceConnect == true)
            TerminateTelephoneCalls(receiver);

        receiver.Comp.LinkedTelephones.Add(source);
        receiver.Comp.Muted = options?.MuteReceiver == true;

        if (options?.ForceConnect == true ||
            (options?.ForceJoin == true &&
            receiver.Comp.CurrentState == TelephoneState.InCall))
        {
            CommenceTelephoneCall(source, receiver);
            return;
        }

        SetTelephoneState(source, TelephoneState.Calling);
        SetTelephoneState(receiver, TelephoneState.Ringing);

        var evCall = new TelephoneCallEvent(source, receiver, user);

        RaiseLocalEvent(receiver, ref evCall);
        RaiseLocalEvent(source, ref evCall);
    }

    public void AnswerTelephone(Entity<TelephoneComponent> receiver, EntityUid user)
    {
        if (receiver.Comp.CurrentState != TelephoneState.Ringing)
            return;

        // If the telephone isn't linked, or is linked to more than one telephone,
        // you shouldn't need to answer the call. If you do need to answer,
        // it needs to be handled in a different way
        if (receiver.Comp.LinkedTelephones.Count != 1)
            return;

        var source = receiver.Comp.LinkedTelephones.First();
        CommenceTelephoneCall(source, receiver);
    }

    public void RedirectTelephoneCall(Entity<TelephoneComponent> oldReceiver, Entity<TelephoneComponent> newReceiver, EntityUid user)
    {
        foreach (var linkedTelephone in oldReceiver.Comp.LinkedTelephones)
        {
            linkedTelephone.Comp.LinkedTelephones.Add(newReceiver);
            linkedTelephone.Comp.LinkedTelephones.Remove(oldReceiver);

            CommenceTelephoneCall(linkedTelephone, newReceiver);
        }

        oldReceiver.Comp.LinkedTelephones.Clear();
        EndTelephoneCalls(oldReceiver);
    }

    private void CommenceTelephoneCall(Entity<TelephoneComponent> source, Entity<TelephoneComponent> receiver)
    {
        SetTelephoneState(source, TelephoneState.InCall);
        SetTelephoneState(receiver, TelephoneState.InCall);

        var evCallCommenced = new TelephoneCallCommencedEvent(source, receiver);

        RaiseLocalEvent(source, ref evCallCommenced);
        RaiseLocalEvent(receiver, ref evCallCommenced);
    }

    public void EndTelephoneCalls(Entity<TelephoneComponent> telephone)
    {
        var evCallEnded = new TelephoneCallEndedEvent(telephone);

        HandleEndingTelephoneCalls(telephone, TelephoneState.EndingCall, evCallEnded);
    }

    public void TerminateTelephoneCalls(Entity<TelephoneComponent> telephone)
    {
        var evCallTerminated = new TelephoneCallTerminatedEvent();

        HandleEndingTelephoneCalls(telephone, TelephoneState.Idle, evCallTerminated);
    }

    private void HandleEndingTelephoneCalls<T>(Entity<TelephoneComponent> telephone, TelephoneState newState, T ev) where T : notnull
    {
        if (telephone.Comp.CurrentState == newState)
            return;

        foreach (var linkedTelephone in telephone.Comp.LinkedTelephones)
        {
            if (!linkedTelephone.Comp.LinkedTelephones.Remove(telephone))
                continue;

            if (!IsTelephoneEngaged(linkedTelephone))
                EndTelephoneCalls(linkedTelephone);

            RaiseLocalEvent(linkedTelephone, ref ev);
        }

        telephone.Comp.LinkedTelephones.Clear();
        telephone.Comp.Muted = false;
        SetTelephoneState(telephone, newState);

        RaiseLocalEvent(telephone, ref ev);
    }

    public void SendTelephoneMessage(EntityUid messageSource, string message, Entity<TelephoneComponent> source, bool escapeMarkup = true)
    {
        if (!IsTelephoneEngaged(source) ||
            !this.IsPowered(source, EntityManager))
            return;

        var evt = new TransformSpeakerNameEvent(messageSource, MetaData(messageSource).EntityName);
        RaiseLocalEvent(messageSource, evt);

        var name = evt.VoiceName;
        name = FormattedMessage.EscapeText(name);

        SpeechVerbPrototype speech;
        if (evt.SpeechVerb != null && _prototype.TryIndex(evt.SpeechVerb, out var evntProto))
            speech = evntProto;
        else
            speech = _chat.GetSpeechVerb(messageSource, message);

        var content = escapeMarkup
            ? FormattedMessage.EscapeText(message)
            : message;

        var wrappedMessage = Loc.GetString(speech.Bold ? "chat-telephone-message-wrap-bold" : "chat-telephone-message-wrap",
            ("color", Color.White),
            ("fontType", speech.FontId),
            ("fontSize", speech.FontSize),
            ("verb", Loc.GetString(_random.Pick(speech.SpeechVerbStrings))),
            ("name", name),
            ("message", content));

        var chat = new ChatMessage(
            ChatChannel.Local,
            message,
            wrappedMessage,
            NetEntity.Invalid,
            null);

        var chatMsg = new MsgChatMessage { Message = chat };

        var evSentMessage = new TelephoneMessageSentEvent(message, messageSource, source, chatMsg);
        RaiseLocalEvent(source, ref evSentMessage);

        var evReceivedMessage = new TelephoneMessageReceivedEvent(message, messageSource, source, chatMsg);

        foreach (var receiver in source.Comp.LinkedTelephones)
        {
            if (!IsSourceInRangeOfReceiver(source, receiver))
                continue;

            RaiseLocalEvent(receiver, ref evReceivedMessage);
        }

        if (name != Name(messageSource))
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Telephone message from {ToPrettyString(messageSource):user} as {name} on {source}: {message}");
        else
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Telephone message from {ToPrettyString(messageSource):user} on {source}: {message}");

        _replay.RecordServerMessage(chat);
    }

    private void SetTelephoneState(Entity<TelephoneComponent> telephone, TelephoneState newState)
    {
        telephone.Comp.CurrentState = newState;
        telephone.Comp.StateStartTime = _timing.RealTime;

        _appearanceSystem.SetData(telephone, TelephoneVisuals.Key, telephone.Comp.CurrentState);

        if (telephone.Comp.CurrentState == TelephoneState.InCall)
        {
            if (!HasComp<ActiveListenerComponent>(telephone))
            {
                var activeListener = AddComp<ActiveListenerComponent>(telephone);
                activeListener.Range = telephone.Comp.ListeningRange;
            }

            return;
        }

        if (HasComp<ActiveListenerComponent>(telephone))
            RemComp<ActiveListenerComponent>(telephone);
    }

    public bool IsSourceAbleToConnectToReceiver(Entity<TelephoneComponent> source, Entity<TelephoneComponent> receiver)
    {
        if (source == receiver ||
            IsTelephoneEngaged(source) ||
            !this.IsPowered(source, EntityManager) ||
            !IsSourceInRangeOfReceiver(source, receiver))
        {
            return false;
        }

        return true;
    }

    public bool IsSourceInRangeOfReceiver(Entity<TelephoneComponent> source, Entity<TelephoneComponent> receiver)
    {
        var sourceXform = Transform(source);
        var receiverXform = Transform(receiver);

        switch (source.Comp.TransmissionRange)
        {
            case TelephoneRange.Grid:
                if (sourceXform.GridUid == null || receiverXform.GridUid != sourceXform.GridUid)
                    return false;
                break;

            case TelephoneRange.Map:
                if (sourceXform.MapID != receiverXform.MapID)
                    return false;
                break;
        }

        return true;
    }

    public bool IsTelephoneEngaged(Entity<TelephoneComponent> telephone)
    {
        return telephone.Comp.LinkedTelephones.Any();
    }

    public bool IsSourceConnectedToReceiver(Entity<TelephoneComponent> source, Entity<TelephoneComponent> receiver)
    {
        return source.Comp.LinkedTelephones.Contains(receiver);
    }
}
