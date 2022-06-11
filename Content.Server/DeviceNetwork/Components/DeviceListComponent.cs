using Content.Server.DeviceNetwork.Systems;

namespace Content.Server.DeviceNetwork.Components;

[RegisterComponent]
[Access(typeof(DeviceListSystem))]
public sealed class DeviceListComponent : Component
{
    /// <summary>
    /// The list of devices can or can't connect to, depending on the <see cref="IsAllowList"/> field.
    /// </summary>
    [DataField("devices")]
    public HashSet<EntityUid> Devices = new();

    /// <summary>
    /// Whether the device list is used as an allow or deny list
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("isAllowList")]
    public bool IsAllowList = true;

    /// <summary>
    /// Whether this device list also handles incoming device net packets
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("handleIncoming")]
    public bool HandleIncomingPackets = false;
}
