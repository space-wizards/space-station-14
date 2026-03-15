using Robust.Shared.GameStates;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// Marker component indicating this camera is currently being jammed by an AI Camera Jammer
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AiCameraJammedComponent : Component
{
    /// <summary>
    /// Set of jammer entities currently jamming this camera.
    /// Allows multiple jammers to affect the same camera.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> JammingSources = new();
}
