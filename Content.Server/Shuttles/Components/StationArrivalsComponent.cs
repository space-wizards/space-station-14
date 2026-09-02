using Content.Server.Shuttles.Systems;
using Content.Shared.Maps;
using Robust.Shared.Utility;

namespace Content.Server.Shuttles.Components;

/// <summary>
/// Added to a station that is available for arrivals shuttles.
/// </summary>
[RegisterComponent, Access(typeof(ArrivalsSystem))]
public sealed partial class StationArrivalsComponent : Component
{
    [DataField]
    public EntityUid? Shuttle;

    [DataField(customTypeSerializer: typeof(MapResPathSerializer))]
    public ResPath ShuttlePath = new("/Maps/Shuttles/arrivals.yml");
}
