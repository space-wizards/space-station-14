using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.DeviceLinking;

/// <summary>
/// A wrapper for a source and a sink pair in device linking.
/// </summary>
[DataRecord, Serializable, NetSerializable]
public readonly partial record struct DeviceLink(
    ProtoId<SourcePortPrototype> SourcePort,
    ProtoId<SinkPortPrototype> SinkPort)
{
    public static implicit operator DeviceLink((ProtoId<SourcePortPrototype> Source, ProtoId<SinkPortPrototype> Sink) tuple)
    {
        return new DeviceLink(tuple.Source, tuple.Sink);
    }

    public static implicit operator ValueTuple<ProtoId<SourcePortPrototype>, ProtoId<SinkPortPrototype>>(DeviceLink link)
    {
        return (link.SourcePort, link.SinkPort);
    }
}
