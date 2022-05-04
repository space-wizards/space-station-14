using Robust.Shared.GameStates;

namespace Content.Shared.SurveillanceCamera;

public abstract class SharedSurveillanceCameraSystem : EntitySystem
{
    /*
    public override void Initialize()
    {
        SubscribeLocalEvent<SharedSurveillanceCameraComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<SharedSurveillanceCameraComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnStartup(EntityUid uid, SharedSurveillanceCameraComponent component, ComponentStartup args)
    {
    }

    private void OnGetState(EntityUid uid, SharedSurveillanceCameraComponent component, ComponentGetState args)
    {
        args.State = new SurveillanceCameraComponentState
        {
            Offset = component.Offset,
            Rotation = component.Rotation,
            Zoom = component.Zoom,
            Scale = component.Scale
        };
    }

    private void OnHandleState(EntityUid uid, SharedSurveillanceCameraComponent component, ComponentHandleState args)
    {
        if (args.Current is not SurveillanceCameraComponentState state)
        {
            return;
        }

        component.Offset = state.Offset;
        component.Rotation = state.Rotation;
        component.Zoom = state.Zoom;
        component.Scale = state.Scale;
    }
    */
}

// This is the event sent to a client when the surveillance camera
// is switched over. This should be paired with an eye subscription
// on the server.
//
// If this is recieved with a valid Camera, the eye should be
// switched over immediately.
public sealed class SurveillanceCameraEyeSwitchEvent : EntityEventArgs
{
    // The EntityUid of the camera in question.
    // If this is null, the player will automatically
    // be kicked back into their original entity.
    public EntityUid? Camera { get; }

    // The entity attached to the client's session.
    // This is so that if the camera is null,
    // we have an entity to switch back to.
    public EntityUid User { get; }

    public SurveillanceCameraEyeSwitchEvent(EntityUid? camera, EntityUid user)
    {
        Camera = camera;
        User = user;
    }
}
