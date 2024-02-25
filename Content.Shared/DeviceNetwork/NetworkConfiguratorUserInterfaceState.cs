using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.DeviceNetwork;

[Serializable, NetSerializable]
public sealed class NetworkConfiguratorUserInterfaceState : BoundUserInterfaceState
{
    public readonly HashSet<(string address, string name)> DeviceList;

    public NetworkConfiguratorUserInterfaceState(HashSet<(string, string)> deviceList)
    {
        DeviceList = deviceList;
    }
}

[Serializable, NetSerializable]
public sealed class DeviceListUserInterfaceState : BoundUserInterfaceState
{
    public readonly HashSet<(string address, string name)> DeviceList;

    public DeviceListUserInterfaceState(HashSet<(string address, string name)> deviceList)
    {
        DeviceList = deviceList;
    }
}

[Serializable, NetSerializable]
public sealed class DeviceLinkUserInterfaceState : BoundUserInterfaceState
{
    public readonly List<SourcePortPrototype> Sources;
    public readonly List<SinkPortPrototype> Sinks;
    public readonly HashSet<(ProtoId<SourcePortPrototype> source, ProtoId<SinkPortPrototype> sink)> Links;
    public readonly List<(string source, string sink)>? Defaults;
    public readonly string SourceAddress;
    public readonly string SinkAddress;

    public DeviceLinkUserInterfaceState(
        List<SourcePortPrototype> sources,
        List<SinkPortPrototype> sinks,
        HashSet<(ProtoId<SourcePortPrototype> source, ProtoId<SinkPortPrototype> sink)> links,
        string sourceAddress,
        string sinkAddress,
        List<(string source, string sink)>? defaults = default)
    {
        Links = links;
        SourceAddress = sourceAddress;
        SinkAddress = sinkAddress;
        Defaults = defaults;
        Sources = sources;
        Sinks = sinks;
    }
}
