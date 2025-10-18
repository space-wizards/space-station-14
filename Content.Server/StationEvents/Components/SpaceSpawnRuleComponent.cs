using Content.Server.StationEvents.Events;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// Component for spawning antags in space around a station.
/// Requires <c>AntagSelectionComponent</c>.
/// </summary>
[RegisterComponent, Access(typeof(SpaceSpawnRule))]
public sealed partial class SpaceSpawnRuleComponent : Component
{
    /// <summary>
    /// Distance that the entity spawns from the station's half AABB radius
    /// </summary>
    [DataField]
    public float SpawnDistance = 20f;

    /// <summary>
    /// Location that was picked.
    /// </summary>
    [DataField]
    public MapCoordinates? Coords;
}
