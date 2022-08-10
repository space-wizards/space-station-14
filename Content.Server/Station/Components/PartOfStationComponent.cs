using Content.Server.GameTicking;

namespace Content.Server.Station.Components;

/// <summary>
///     Added to grids saved in maps to designate them as 'part of a station' and not main grids. I.e. ancillary
///     shuttles for multi-grid stations.
/// </summary>
[RegisterComponent, Access(typeof(GameTicker)), Obsolete("Performs the exact same function as BecomesStationComponent.")]
public sealed class PartOfStationComponent : Component
{
    [DataField("id", required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public string Id = default!;
}
