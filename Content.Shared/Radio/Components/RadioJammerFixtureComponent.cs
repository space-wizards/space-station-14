using Robust.Shared.Map;

namespace Content.Shared.Radio.Components;

/// <summary>
/// Tracks active jammer state for camera jamming.
/// Added while the jammer is activated; removed on deactivation.
/// </summary>
[RegisterComponent]
public sealed partial class RadioJammerFixtureComponent : Component
{
    /// <summary>
    /// AI cameras currently being jammed.
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
    /// </summary>
    [ViewVariables]
    public TimeSpan NextUpdateTime;
}
