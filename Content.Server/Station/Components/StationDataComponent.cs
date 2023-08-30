using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Server.Station.Components;

/// <summary>
/// Stores core information about a station, namely its config and associated grids.
/// All station entities will have this component.
/// </summary>
[RegisterComponent, Access(typeof(StationSystem))]
public sealed partial class StationDataComponent : Component
{
    /// <summary>
    /// The game map prototype, if any, associated with this station.
    /// </summary>
    [DataField("stationConfig")]
    public StationConfig? StationConfig = null;

    /// <summary>
    /// List of all grids this station is part of.
    /// </summary>
    [DataField("grids")]
    public HashSet<EntityUid> Grids = new();
}
