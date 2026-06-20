using Robust.Shared.GameStates;

namespace Content.Shared.DeviceNetwork.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class StationLimitedNetworkComponent : Component
{
    /// <summary>
    /// The station id the device is limited to.
    /// </summary>
    [ViewVariables]
    public EntityUid? StationId;

    /// <summary>
    /// Whether the entity is allowed to receive packets from entities that are not tied to any station
    /// </summary>
    [DataField]
    public bool AllowNonStationPackets = false;
}
