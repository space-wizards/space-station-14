using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;

namespace Content.Server.SurveillanceCamera;

public sealed class SurveillanceCameraMicrophoneSystem : EntitySystem
{
    [Dependency] private SharedInteractionSystem _interactionSystem = default!;

    public bool CanListen(EntityUid source, EntityUid speaker, SurveillanceCameraMicrophoneComponent? microphone = null)
    {
        if (!Resolve(source, ref microphone))
        {
            return false;
        }

        return microphone.Enabled
               && !microphone.BlacklistedComponents.IsValid(speaker)
               && _interactionSystem.InRangeUnobstructed(source, speaker, range: microphone.ListenRange);
    }
    public void RelayEntityMessage(EntityUid source, EntityUid speaker, string message, SurveillanceCameraComponent? camera = null)
    {
        if (!Resolve(source, ref camera))
        {
            return;
        }

        var wrapped = Loc.GetString("surveillance-camera-microphone-message", ("speaker", speaker), ("message", message));
        var ev = new SurveillanceCameraSpeechSendEvent(speaker, message, wrapped);

        foreach (var monitor in camera.ActiveMonitors)
        {
            RaiseLocalEvent(monitor, ev);
        }
    }
}

public sealed class SurveillanceCameraSpeechSendEvent : EntityEventArgs
{
    public EntityUid Speaker { get; }
    public string OriginalMessage { get; }
    public string WrappedMessage { get; }

    public SurveillanceCameraSpeechSendEvent(EntityUid speaker, string originalMessage, string wrappedMessage)
    {
        Speaker = speaker;
        OriginalMessage = originalMessage;
        WrappedMessage = wrappedMessage;
    }
}

