using Content.Shared.SurveillanceCamera;

namespace Content.Server.SurveillanceCamera.Components;

/// <summary>
/// Stores surveillance camera data for the camera map.
/// </summary>
[RegisterComponent]
[Access(typeof(SurveillanceCameraMapSystem))]
public sealed partial class SurveillanceCameraMapComponent : Component
{
    /// <summary>
    /// Dictionary of cameras on the current grid.
    /// </summary>
    [ViewVariables]
    public Dictionary<NetEntity, CameraMarker> Cameras = [];
}

/// <summary>
/// Raised on a grid when its camera markers change.
/// </summary>
[ByRefEvent]
public readonly record struct SurveillanceCameraMapMarkersChangedEvent;
