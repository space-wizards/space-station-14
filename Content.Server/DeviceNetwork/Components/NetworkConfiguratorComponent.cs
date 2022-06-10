using Content.Server.DeviceNetwork.Systems;
using Content.Shared.Sound;

namespace Content.Server.DeviceNetwork.Components;

[RegisterComponent]
[Access(typeof(NetworkConfiguratorSystem))]
public sealed class NetworkConfiguratorComponent : Component
{
    /// <summary>
    /// The list of devices stored in the configurator-
    /// </summary>
    [DataField("devices")]
    public Dictionary<string, EntityUid> Devices = new();

    /// <summary>
    /// The entity containing a <see cref="DeviceListComponent"/> this configurator is currently interacting with
    /// </summary>
    [DataField("activeDeviceList")]
    public EntityUid? ActiveDeviceList = null;

    [DataField("soundNoAccess")]
    public SoundSpecifier SoundNoAccess = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");
}
