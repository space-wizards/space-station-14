using Content.Server.StationEvents.Events;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// Component for spawning antags in space around a station.
/// Requires <c>AntagSelectionComponent</c>.
/// </summary>
[RegisterComponent, Access(typeof(OutsideStationAntagSpawnRule))]
public sealed partial class OutsideStationAntagSpawnComponent : Component
{
    /// <summary>
    /// Location that was picked.
    /// </summary>
    [DataField]
    public MapCoordinates? Coords;
}
