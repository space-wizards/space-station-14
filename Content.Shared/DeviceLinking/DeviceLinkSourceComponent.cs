using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.DeviceLinking;

[RegisterComponent, NetworkedComponent]
[Access(typeof(DeviceLinkSystem))]
public sealed partial class DeviceLinkSourceComponent : Component
{
    /// <summary>
    /// The ports the device link source sends signals from
    /// </summary>
    [DataField]
    public HashSet<ProtoId<SourcePortPrototype>> Ports = new();

    /// <summary>
    /// Dictionary mapping each port to a set of linked sink entities.
    /// </summary>
    [ViewVariables] // This is not YAML serialized as it can be constructed from LinkedPorts
    public Dictionary<ProtoId<SourcePortPrototype>, HashSet<EntityUid>> Outputs = new();

    /// <summary>
    /// If set to High or Low, the last signal state for a given port.
    /// Used when linking ports of devices that are currently outputting a signal.
    /// Only set by <c>DeviceLinkSystem.SendSignal</c>.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<SourcePortPrototype>, bool> LastSignals = new();

    /// <summary>
    /// The list of source to sink ports for each linked sink entity for easier managing of links
    /// </summary>
    [DataField]
    public Dictionary<EntityUid, HashSet<(ProtoId<SourcePortPrototype> Source, ProtoId<SinkPortPrototype> Sink)>> LinkedPorts = new();

    /// <summary>
    ///     Limits the range devices can be linked across.
    /// </summary>
    [DataField]
    public float Range = 30f;
}

[Serializable, NetSerializable]
public sealed class DeviceLinkSourceComponentState : ComponentState
{
    // This component state exists just because of this   V   nested EntityUid hashset. Someone send help.
    public Dictionary<ProtoId<SourcePortPrototype>, HashSet<NetEntity>> Outputs;
    public Dictionary<ProtoId<SourcePortPrototype>, bool> LastSignals;
    public Dictionary<NetEntity, HashSet<(ProtoId<SourcePortPrototype> Source, ProtoId<SinkPortPrototype> Sink)>> LinkedPorts;

    public DeviceLinkSourceComponentState(
        Dictionary<ProtoId<SourcePortPrototype>, HashSet<NetEntity>> outputs,
        Dictionary<ProtoId<SourcePortPrototype>, bool> lastSignals,
        Dictionary<NetEntity, HashSet<(ProtoId<SourcePortPrototype> Source, ProtoId<SinkPortPrototype> Sink)>> linkedPorts)
    {
        Outputs = outputs;
        LastSignals = lastSignals;
        LinkedPorts = linkedPorts;
    }
}
