using Content.Shared.Atmos.Monitor;
using Content.Shared.DeviceNetwork;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server.Atmos.Monitor.Payloads;

/// <summary>
/// Interface for <see cref="AtmosAlarmableSourcePayload{T}"/> without the typed parameter.
/// </summary>
public interface IAtmosAlarmableSourcePayload
{
    HashSet<ProtoId<TagPrototype>> Source { get; set; }
}

/// <summary>
/// A network payload that has a whitelist of tags that should listen to it.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class AtmosAlarmableSourcePayload<T> : NetworkPayloadBase<T>, IAtmosAlarmableSourcePayload where T : NetworkPayloadBase<T>
{
    [DataField]
    public HashSet<ProtoId<TagPrototype>> Source { get; set; } = new();
}

/// <summary>
/// Broadcasts an atmos alarm.
/// </summary>
public sealed partial class AtmosAlarmPayload : AtmosAlarmableSourcePayload<AtmosAlarmPayload>
{
    [DataField]
    public AtmosAlarmType Type;

    [DataField]
    public AtmosMonitorThresholdTypeFlags TrippedThresholds;
}

/// <summary>
/// Synchronizes the data of an atmos alarmable to all other alarmable devices.
/// </summary>
public sealed partial class AtmosAlarmableSyncAlertsPayload : AtmosAlarmableSourcePayload<AtmosAlarmableSyncAlertsPayload>
{
    [DataField]
    public Dictionary<string, AtmosAlarmType> AlarmStates = new();
}

/// <summary>
/// Resets the state of an atmos alarmable to Normal.
/// </summary>
public sealed partial class AtmosAlarmableResetAllPayload : AtmosAlarmableSourcePayload<AtmosAlarmableResetAllPayload>;
