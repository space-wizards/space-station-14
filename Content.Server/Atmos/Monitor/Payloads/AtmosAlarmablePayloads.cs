using Content.Shared.Atmos.Monitor;
using Content.Shared.DeviceNetwork;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.Atmos.Monitor.Payloads;

[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class AtmosAlarmableSourcePayload : HandledNetworkPayload
{
    [DataField]
    public HashSet<ProtoId<TagPrototype>> Source = new();
}

[Serializable, NetSerializable]
public sealed partial class AtmosAlarmPayload : AtmosAlarmableSourcePayload
{
    [DataField]
    public AtmosAlarmType Type;

    [DataField]
    public AtmosMonitorThresholdTypeFlags TrippedThresholds;
}

[Serializable, NetSerializable]
public sealed partial class AtmosAlarmableSyncAlertsPayload : AtmosAlarmableSourcePayload
{
    [DataField]
    public Dictionary<string, AtmosAlarmType> AlarmStates = new();
}

[Serializable, NetSerializable]
public sealed partial class AtmosAlarmableResetAllPayload : AtmosAlarmableSourcePayload;
