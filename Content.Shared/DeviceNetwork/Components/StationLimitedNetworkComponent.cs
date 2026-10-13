using Content.Shared.Station.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.DeviceNetwork.Components;

/// <summary>
/// When applied together with <see cref="StationTrackerComponent"/>, will limit
/// the sending and optionally receiving of device packets to a currently tracked station.
/// </summary>
// TODO: Reconsider this component entirely in the future, possibly by adding new retranslator machines to communicate received packets between grids
[RegisterComponent, NetworkedComponent]
public sealed partial class StationLimitedNetworkComponent : Component
{
    /// <summary>
    /// Whether the entity is allowed to receive packets from entities that are not tied to any station
    /// </summary>
    [DataField]
    public bool AllowNonStationPackets;
}
