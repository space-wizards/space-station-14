using Content.Server.Chat.Systems;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Player;
using static Content.Server.Chat.Systems.ChatSystem;

namespace Content.Server.SurveillanceCamera;

public sealed class SurveillanceCameraMicrophoneSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xforms = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SurveillanceCameraMicrophoneComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SurveillanceCameraMicrophoneComponent, ListenEvent>(RelayEntityMessage);
        SubscribeLocalEvent<SurveillanceCameraMicrophoneComponent, ListenAttemptEvent>(CanListen);
        SubscribeLocalEvent<ExpandICChatRecipientsEvent>(OnExpandRecipients);
    }

    private void OnExpandRecipients(ExpandICChatRecipientsEvent ev)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();
        var sourceXform = Transform(ev.Source);
        var sourcePos = _xforms.GetWorldPosition(sourceXform, xformQuery);

        // This function ensures that chat popups appear on camera views that have connected microphones.
        foreach (var (_, __, camera, xform) in EntityQuery<SurveillanceCameraMicrophoneComponent, ActiveListenerComponent, SurveillanceCameraComponent, TransformComponent>())
        {
            if (camera.ActiveViewers.Count == 0)
                continue;

            // get range to camera. This way wispers will still appear as obfuscated if they are too far from the camera's microphone
            var range = (xform.MapID != sourceXform.MapID)
                ? -1
                : (sourcePos - _xforms.GetWorldPosition(xform, xformQuery)).Length();

            if (range < 0 || range > ev.VoiceRange)
                continue;

            foreach (var viewer in camera.ActiveViewers)
            {
                // if the player has not already received the chat message, send it to them but don't log it to the chat
                // window. This is simply so that it appears in camera.
                if (TryComp(viewer, out ActorComponent? actor))
                    ev.Recipients.TryAdd(actor.PlayerSession, new ICChatRecipientData(range, false, true));
            }
        }
    }

    private void OnInit(EntityUid uid, SurveillanceCameraMicrophoneComponent component, ComponentInit args)
    {
        if (component.Enabled)
            EnsureComp<ActiveListenerComponent>(uid).Range = component.Range;
        else
            RemCompDeferred<ActiveListenerComponent>(uid);
    }

    public void CanListen(EntityUid uid, SurveillanceCameraMicrophoneComponent microphone, ListenAttemptEvent args)
    {
        // TODO maybe just make this a part of ActiveListenerComponent?
        if (_whitelistSystem.IsBlacklistPass(microphone.Blacklist, args.Source))
            args.Cancel();
    }

    public void RelayEntityMessage(EntityUid uid, SurveillanceCameraMicrophoneComponent component, ListenEvent args)
    {
        if (!TryComp(uid, out SurveillanceCameraComponent? camera))
            return;

        var ev = new SurveillanceCameraSpeechSendEvent(args.Source, args.Message);

        foreach (var monitor in camera.ActiveMonitors)
        {
            RaiseLocalEvent(monitor, ev);
        }
    }

    public void SetEnabled(EntityUid uid, bool value, SurveillanceCameraMicrophoneComponent? microphone = null)
    {
        if (!Resolve(uid, ref microphone))
            return;

        if (value == microphone.Enabled)
            return;

        microphone.Enabled = value;

        if (value)
            EnsureComp<ActiveListenerComponent>(uid).Range = microphone.Range;
        else
            RemCompDeferred<ActiveListenerComponent>(uid);
    }
}

public sealed class SurveillanceCameraSpeechSendEvent : EntityEventArgs
{
    public EntityUid Speaker { get; }
    public string Message { get; }

    public SurveillanceCameraSpeechSendEvent(EntityUid speaker, string message)
    {
        Speaker = speaker;
        Message = message;
    }
}

