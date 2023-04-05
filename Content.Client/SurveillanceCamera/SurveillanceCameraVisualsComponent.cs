using Content.Shared.SurveillanceCamera;

namespace Content.Client.SurveillanceCamera;

// Dummy component so that targetted events work on client for
// appearance events.
[RegisterComponent]
public sealed class SurveillanceCameraVisualsComponent : Component
{
    [DataField("sprites")]
    public readonly Dictionary<SurveillanceCameraVisuals, string> CameraSprites = new();
}
