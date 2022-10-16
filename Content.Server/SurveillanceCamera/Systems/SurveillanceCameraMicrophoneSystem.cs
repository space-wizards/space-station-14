using Content.Server.Speech;
using Content.Server.Speech.Components;
using Content.Shared.Interaction;

namespace Content.Server.SurveillanceCamera;

public sealed class SurveillanceCameraMicrophoneSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SurveillanceCameraMicrophoneComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<SurveillanceCameraMicrophoneComponent, ListenEvent>(RelayEntityMessage);
        SubscribeLocalEvent<SurveillanceCameraMicrophoneComponent, ListenAttemptEvent>(CanListen);
    }

    private void OnInit(EntityUid uid, SurveillanceCameraMicrophoneComponent component, ComponentInit args)
    {
        if (component.Enabled)
            EnsureComp<ActiveListenerComponent>(uid).Range = component.ListenRange;
        else
            RemCompDeferred<ActiveListenerComponent>(uid);
    }

    public void CanListen(EntityUid uid, SurveillanceCameraMicrophoneComponent microphone, ListenAttemptEvent args)
    {
        // TODO maybe just make this a part of ActiveListenerComponent?
        if (microphone.BlacklistedComponents.IsValid(args.Source))
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
            EnsureComp<ActiveListenerComponent>(uid).Range = microphone.ListenRange;
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

