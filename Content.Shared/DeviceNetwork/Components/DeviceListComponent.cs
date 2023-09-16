using Content.Shared.DeviceNetwork.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.DeviceNetwork.Components;

[RegisterComponent]
[NetworkedComponent]
[Access(typeof(SharedDeviceListSystem))]
public sealed partial class DeviceListComponent : Component
{
    /// <summary>
    /// The list of devices can or can't connect to, depending on the <see cref="IsAllowList"/> field.
    /// </summary>
    [DataField("devices")]
    public HashSet<EntityUid> Devices = new();

    /// <summary>
    /// The limit of devices that can be linked to this device list.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("deviceLimit")]
    public int DeviceLimit = 32;

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

[Serializable, NetSerializable]
public sealed class DeviceListComponentState : ComponentState
{
    public readonly HashSet<NetEntity> Devices;
    public readonly bool IsAllowList;
    public readonly bool HandleIncomingPackets;

    public DeviceListComponentState(HashSet<NetEntity> devices, bool isAllowList, bool handleIncomingPackets)
    {
        Devices = devices;
        IsAllowList = isAllowList;
        HandleIncomingPackets = handleIncomingPackets;
    }
}
