using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.DeviceNetwork;

[Serializable, NetSerializable]
public sealed class NetworkConfiguratorUserInterfaceState : BoundUserInterfaceState
{
    public readonly HashSet<(LocDeviceAddress address, string name)> DeviceList;

    public NetworkConfiguratorUserInterfaceState(HashSet<(LocDeviceAddress, string)> deviceList)
    {
        DeviceList = deviceList;
    }
}

[Serializable, NetSerializable]
public sealed class DeviceListUserInterfaceState : BoundUserInterfaceState
{
    public readonly HashSet<(LocDeviceAddress address, string name)> DeviceList;

    public DeviceListUserInterfaceState(HashSet<(LocDeviceAddress address, string name)> deviceList)
    {
        DeviceList = deviceList;
    }
}

[Serializable, NetSerializable]
public sealed class DeviceLinkUserInterfaceState : BoundUserInterfaceState
{
    public readonly ProtoId<SourcePortPrototype>[] Sources;
    public readonly ProtoId<SinkPortPrototype>[] Sinks;
    public readonly HashSet<DeviceLink> Links;
    public readonly List<DeviceLink>? Defaults;
    public readonly DeviceAddress SourceAddressId;
    public readonly DeviceAddress SinkAddressId;
    public readonly string SourceAddress;
    public readonly string SinkAddress;

    public DeviceLinkUserInterfaceState(
        ProtoId<SourcePortPrototype>[] sources,
        ProtoId<SinkPortPrototype>[] sinks,
        HashSet<DeviceLink> links,
        DeviceAddress sourceAddressId,
        DeviceAddress sinkAddressId,
        string sourceAddress,
        string sinkAddress,
        List<DeviceLink>? defaults = default)
    {
        Links = links;
        SourceAddressId = sourceAddressId;
        SinkAddressId = sinkAddressId;
        SourceAddress = sourceAddress;
        SinkAddress = sinkAddress;
        Defaults = defaults;
        Sources = sources;
        Sinks = sinks;
    }
}
