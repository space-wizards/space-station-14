using Robust.Shared.GameStates;

namespace Content.Shared.DeviceNetwork.Components.Networks;

[RegisterComponent, NetworkedComponent]
public sealed partial class WirelessNetworkManagerComponent : Component
{
    /// <summary>
    /// Map ID that this network controls.
    /// </summary>
    [DataField]
    public EntityUid? MapId;
}
