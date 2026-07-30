using Content.Shared.Atmos.Monitor;
using Content.Shared.DeviceNetwork;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server.Atmos.Monitor.Payloads;

[ImplicitDataDefinitionForInheritors]
public partial interface IAtmosAlarmableSourcePayload : INetworkPayload
{
    HashSet<ProtoId<TagPrototype>> Source { get; set; }
}

/// <summary>
/// Broadcasts an atmos alarm.
/// </summary>
public partial record struct AtmosAlarmPayload : IAtmosAlarmableSourcePayload
{
    [DataField]
    public AtmosAlarmType Type;

    [DataField]
    public AtmosMonitorThresholdTypeFlags TrippedThresholds;

    [DataField]
    public HashSet<ProtoId<TagPrototype>> Source { get; set; } = new();
}

/// <summary>
/// Synchronizes the data of an atmos alarmable to all other alarmable devices.
/// </summary>
public partial record struct AtmosAlarmableSyncAlertsPayload : IAtmosAlarmableSourcePayload
{
    [DataField]
    public Dictionary<DeviceAddress, AtmosAlarmType> AlarmStates = new();

    [DataField]
    public HashSet<ProtoId<TagPrototype>> Source { get; set; } = new();
}

/// <summary>
/// Resets the state of an atmos alarmable to Normal.
/// </summary>
public partial record struct AtmosAlarmableResetAllPayload : IAtmosAlarmableSourcePayload
{
    [DataField]
    public HashSet<ProtoId<TagPrototype>> Source { get; set; } = new();
}
