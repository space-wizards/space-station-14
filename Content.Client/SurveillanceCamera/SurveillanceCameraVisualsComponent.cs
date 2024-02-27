using Content.Shared.SurveillanceCamera;

namespace Content.Client.SurveillanceCamera;

// Dummy component so that targeted events work on client for
// appearance events.
[RegisterComponent]
public sealed partial class SurveillanceCameraVisualsComponent : Component
{
    [DataField("sprites")]
    public Dictionary<SurveillanceCameraVisuals, string> CameraSprites = new();
}
