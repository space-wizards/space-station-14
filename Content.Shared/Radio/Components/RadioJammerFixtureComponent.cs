using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;

namespace Content.Shared.Radio.Components;

/// <summary>
/// Tracks collision fixture for unified jammer range detection.
/// Used by both radio jamming and AI camera jamming systems.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RadioJammerFixtureComponent : Component
{
    public const string FixtureID = "radio-jammer-range-fixture";

    /// <summary>
    /// All entities currently within jammer range (via collision).
    /// </summary>
    [ViewVariables]
    public readonly Dictionary<EntityUid, PhysicsComponent> CollidingEntities = new();

    /// <summary>
    /// AI cameras currently being jammed.
    /// Subset of CollidingEntities that have StationAiVisionComponent.
    /// </summary>
    [ViewVariables]
    public readonly HashSet<EntityUid> JammedCameras = new();

    /// <summary>
    /// Last known map coordinates of the jammer.
    /// Used to detect position changes when jammer is being held/carried.
    /// </summary>
    [ViewVariables]
    public MapCoordinates? LastPosition;

    /// <summary>
    /// Next time to check for position changes and update jammed cameras.
    /// Used to throttle queries
    /// </summary>
    [ViewVariables]
    public TimeSpan NextUpdateTime;
}
