using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

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

[Serializable, NetSerializable]
public sealed class NetworkConfiguratorComponentState : ComponentState
{
    public readonly EntityUid? ActiveDeviceList;

    public NetworkConfiguratorComponentState(EntityUid? activeDeviceList)
    {
        ActiveDeviceList = activeDeviceList;
    }
}
