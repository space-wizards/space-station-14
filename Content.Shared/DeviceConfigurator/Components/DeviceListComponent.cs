using Content.Shared.DeviceConfigurator.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.DeviceConfigurator.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
[Access(typeof(DeviceListSystem))]
public sealed partial class DeviceListComponent : Component
{
    /// <summary>
    /// The list of devices can or can't connect to, depending on the <see cref="IsAllowList"/> field.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Devices = new();

    /// <summary>
    /// The limit of devices that can be linked to this device list.
    /// </summary>
    [DataField]
    public int DeviceLimit = 32;

    /// <summary>
    /// Whether the device list is used as an allow or deny list
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsAllowList = true;

    /// <summary>
    /// Whether this device list also handles incoming device net packets
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HandleIncomingPackets;

    [DataField, AutoNetworkedField]
    [Access(typeof(NetworkConfiguratorSystem))]
    public HashSet<EntityUid> Configurators = new();
}
