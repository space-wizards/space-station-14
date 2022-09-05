using Robust.Shared.GameStates;

namespace Content.Shared.DeviceNetwork;

[NetworkedComponent]
public abstract class SharedNetworkConfiguratorComponent : Component
{
    /// <summary>
    /// The entity containing a <see cref="DeviceListComponent"/> this configurator is currently interacting with
    /// </summary>
    [DataField("activeDeviceList")]
    public EntityUid? ActiveDeviceList = null;
}
