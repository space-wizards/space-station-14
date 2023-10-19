using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Components;

/// <summary>
/// Spawns Central Command (emergency destination) for a station.
/// </summary>
[RegisterComponent]
public sealed partial class StationCentcomComponent : Component
{
    /// <summary>
    /// Crude shuttle offset spawning.
    /// </summary>
    [DataField("shuttleIndex")]
    public float ShuttleIndex;

    [DataField("map")]
    public ResPath Map = new("/Maps/centcom.yml");

    /// <summary>
    /// Centcom entity that was loaded.
    /// </summary>
    [DataField("entity")]
    public EntityUid Entity = EntityUid.Invalid;

    public MapId MapId = MapId.Nullspace;
}
