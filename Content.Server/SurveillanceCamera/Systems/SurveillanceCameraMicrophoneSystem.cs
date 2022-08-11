using Content.Shared.IdentityManagement;

namespace Content.Server.SurveillanceCamera;

public sealed class SurveillanceCameraMicrophoneSystem : EntitySystem
{
    public void RelayEntityMessage(EntityUid source, EntityUid speaker, string message, SurveillanceCameraComponent? camera = null)
    {
        if (!Resolve(source, ref camera))
        {
            return;
        }

        message = Loc.GetString("surveillance-camera-microphone-message", ("speaker", speaker), ("message", message));
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

