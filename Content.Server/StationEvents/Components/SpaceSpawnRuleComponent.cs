using Content.Server.StationEvents.Events;
using Content.Shared.Antag.Components;
using Robust.Shared.Map;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// Component for spawning antags in space around a station.
/// </summary>
/// <remarks>
/// Requires <see cref="AntagSelectionComponent"/>.
/// </remarks>
[RegisterComponent, Access(typeof(SpaceSpawnRule))]
public sealed partial class SpaceSpawnRuleComponent : Component
{
    /// <summary>
    /// Final distance that the entity should spawn from the station's grid.
    /// </summary>
    [DataField]
    public float SpawnDistance = 20f;

    /// <summary>
    /// The distance we should check around each spawn point for grids.
    /// </summary>
    /// <remarks>
    /// Should be less than <see cref="SpawnDistance"/>
    /// </remarks>
    [DataField]
    public float ClearDistance = 5f;

    /// <summary>
    /// Number of attempts to check around the station, evenly spaced around a circle.
    /// </summary>
    [DataField]
    public int MaxAttempts = 5;

    /// <summary>
    /// Location that was picked.
    /// </summary>
    [DataField]
    public MapCoordinates? Coords;
}
