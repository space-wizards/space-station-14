namespace Content.Shared.DeviceNetwork;

public abstract class SharedNetworkConfiguratorComponent : Component
{
    /// <summary>
    /// The entity containing a <see cref="DeviceListComponent"/> this configurator is currently interacting with
    /// </summary>
    [DataField("activeDeviceList")]
    public EntityUid? ActiveDeviceList = null;
}
