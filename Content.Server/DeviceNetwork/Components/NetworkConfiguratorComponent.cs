using Content.Server.DeviceNetwork.Systems;
using Content.Shared.DeviceNetwork;
using Robust.Shared.Audio;

namespace Content.Server.DeviceNetwork.Components;

[RegisterComponent]
[Access(typeof(NetworkConfiguratorSystem))]
public sealed class NetworkConfiguratorComponent : SharedNetworkConfiguratorComponent
{
    /// <summary>
    /// The list of devices stored in the configurator-
    /// </summary>
    [DataField("devices")]
    public Dictionary<string, EntityUid> Devices = new();

    [DataField("soundNoAccess")]
    public SoundSpecifier SoundNoAccess = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");
}
