using Content.Shared.IdentityManagement;

namespace Content.Server.SurveillanceCamera;

public sealed class SurveillanceCameraMicrophoneSystem : EntitySystem
{
    public override void Initialize()
    {

    }

    private void OnEntitySpeak()
    {
        // check if the speaking entity has an offending component,
        // and if it does, disregard
    }

    private void RelayEntityMessage(EntityUid source, EntityUid speaker, string message, SurveillanceCameraComponent? camera)
    {
        if (!Resolve(speaker, ref camera))
        {
            return;
        }

        var ev = new SurveillanceCameraSpeechSendEvent(message);

        foreach (var monitor in camera.ActiveMonitors)
        {
            RaiseLocalEvent(monitor, ev);
        }
    }
}

public sealed class SurveillanceCameraSpeechSendEvent : EntityEventArgs
{
    public string Message { get; }

    public SurveillanceCameraSpeechSendEvent(string message)
    {
        Message = message;
    }
}

