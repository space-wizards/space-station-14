using Content.Shared.GameTicking;
using Robust.Shared.GameStates;

namespace Content.Shared.Station.Components;

/// <summary>
///     Added to grids saved in maps to designate that they are the 'main station' grid.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedGameTicker))]
public sealed partial class BecomesStationComponent : Component
{
    /// <summary>
    ///     Mapping only. Should use StationIds in all other
    ///     scenarios.
    /// </summary>
    [DataField("id", required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public string Id = default!;
}
