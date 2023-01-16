using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.DeviceNetwork;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDeviceListSystem))]
public sealed class DeviceListComponent : Component
{
    /// <summary>
    /// The list of devices can or can't connect to, depending on the <see cref="IsAllowList"/> field.
    /// </summary>
    [DataField("devices")]
    [AutoNetworkedField]
    public HashSet<EntityUid> Devices = new();

    /// <summary>
    /// The limit of devices that can be linked to this device list.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("deviceLimit")]
    [AutoNetworkedField]
    public int DeviceLimit = 32;

    /// <summary>
    /// Whether the device list is used as an allow or deny list
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("isAllowList")]
    [AutoNetworkedField]
    public bool IsAllowList = true;

    /// <summary>
    /// Whether this device list also handles incoming device net packets
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("handleIncoming")]
    [AutoNetworkedField]
    public bool HandleIncomingPackets = false;
}
