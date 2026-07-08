using Content.Shared.Atmos.Monitor;
using Content.Shared.DeviceNetwork;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server.Atmos.Monitor.Payloads;

public interface IAtmosAlarmableSourcePayload
{
    HashSet<ProtoId<TagPrototype>> Source { get; set; }
}

[ImplicitDataDefinitionForInheritors]
public abstract partial class AtmosAlarmableSourcePayload<T> : NetworkPayloadBase<T>, IAtmosAlarmableSourcePayload where T : NetworkPayloadBase<T>
{
    [DataField]
    public HashSet<ProtoId<TagPrototype>> Source { get; set; } = new();
}

public sealed partial class AtmosAlarmPayload : AtmosAlarmableSourcePayload<AtmosAlarmPayload>
{
    [DataField]
    public AtmosAlarmType Type;

    [DataField]
    public AtmosMonitorThresholdTypeFlags TrippedThresholds;
}

public sealed partial class AtmosAlarmableSyncAlertsPayload : AtmosAlarmableSourcePayload<AtmosAlarmableSyncAlertsPayload>
{
    [DataField]
    public Dictionary<string, AtmosAlarmType> AlarmStates = new();
}

public sealed partial class AtmosAlarmableResetAllPayload : AtmosAlarmableSourcePayload<AtmosAlarmableResetAllPayload>;
