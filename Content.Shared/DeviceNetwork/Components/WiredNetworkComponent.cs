using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeviceNetwork.Components;

/// <summary>
/// Allows a device to send and receive device network messages over wired connections.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WiredNetworkComponent : Component, ILimitedDeviceComponent
{
    [DataField]
    public ProtoId<DeviceNetworkPrototype>[]? IgnoreNets { get; set; }
}
