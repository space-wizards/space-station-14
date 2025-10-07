using Content.Server.Wires;
using Content.Shared.SurveillanceCamera.Components;
using Content.Shared.Wires;

namespace Content.Server.SurveillanceCamera;

public sealed partial class CameraMapVisibilityWireAction : ComponentWireAction<SurveillanceCameraComponent>
{
    private SurveillanceCameraMapSystem _cameraMapSystem => EntityManager.System<SurveillanceCameraMapSystem>();

    public override string Name { get; set; } = "wire-name-camera-map";
    public override Color Color { get; set; } = Color.Teal;
    public override object StatusKey => "OnMapVisibility";

    public override StatusLightState? GetLightState(Wire wire, SurveillanceCameraComponent component)
    {
        return _cameraMapSystem.IsCameraVisible(wire.Owner)
            ? StatusLightState.On
            : StatusLightState.Off;
    }

    public override bool Cut(EntityUid user, Wire wire, SurveillanceCameraComponent component)
    {
        _cameraMapSystem.SetCameraVisibility(wire.Owner, false);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, SurveillanceCameraComponent component)
    {
        _cameraMapSystem.SetCameraVisibility(wire.Owner, true);
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, SurveillanceCameraComponent component)
    {

    }
}
