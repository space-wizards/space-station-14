using Content.Shared.Silicons.StationAi.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// Marker component indicating this AI camera jammer is currently active
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedAiCameraJammerSystem))]
public sealed partial class ActiveAiCameraJammerComponent : Component
{
    /// <summary>
    /// Set of cameras currently being jammed by this jammer.
    /// Used to restore cameras when they leave range or jammer deactivates.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> JammedCameras = new();

    /// <summary>
    /// Last known map coordinates of the jammer.
    /// Used to detect position changes when jammer is being held/carried.
    /// Since we are using item movement event to track position for performance reasons
    /// and that doesnt fire when its held.
    /// </summary>
    [DataField, AutoNetworkedField]
    public MapCoordinates? LastPosition;

    /// <summary>
    /// Next time to check for position changes and update jammed cameras.
    /// Used to throttle spatial queries to avoid per-frame overhead.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan NextUpdateTime;
}
