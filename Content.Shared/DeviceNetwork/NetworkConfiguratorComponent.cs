using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.DeviceNetwork;

[RegisterComponent]
[NetworkedComponent]
[Access(typeof(SharedNetworkConfiguratorSystem))]
public sealed class NetworkConfiguratorComponent : Component
{
    /// <summary>
    /// The entity containing a <see cref="DeviceListComponent"/> this configurator is currently interacting with
    /// </summary>
    [DataField("activeDeviceList")]
    public EntityUid? ActiveDeviceList = null;

    /// <summary>
    /// The list of devices stored in the configurator-
    /// </summary>
    [DataField("devices")]
    public Dictionary<string, EntityUid> Devices = new();

    [DataField("soundNoAccess")]
    public SoundSpecifier SoundNoAccess = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");
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
