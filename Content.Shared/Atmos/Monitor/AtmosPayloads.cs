using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.DeviceNetwork;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Monitor;

[Serializable, NetSerializable]
public sealed partial class AtmosRegisterDevicePayload : NetworkPayload;

[Serializable, NetSerializable]
public sealed partial class AtmosDeregisterDevicePayload : NetworkPayload;

[Serializable, NetSerializable]
public sealed partial class AtmosSyncDevicePayload : NetworkPayload
{
    [DataField]
    public AtmosDeviceDataPayload? Data;
}

[Serializable, NetSerializable]
public sealed partial class AtmosDeviceSetDataPayload : NetworkPayload
{
    [DataField]
    public AtmosDeviceDataPayload Data;
}

[ImplicitDataDefinitionForInheritors]
[Serializable, NetSerializable]
public abstract partial class AtmosAlarmableSourcePayload : NetworkPayload
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

[Serializable, NetSerializable]
public sealed partial class AtmosMonitorSetThresholdPayload : NetworkPayload
{
    [DataField]
    public AtmosMonitorThresholdType Type;

    [DataField]
    public AtmosAlarmThreshold Threshold;

    [DataField]
    public Gas? Gas;
}

[Serializable, NetSerializable]
public sealed partial class AtmosMonitorSetAllThresholdsPayload : NetworkPayload
{
    [DataField]
    public AtmosSensorDataPayload Data;
}

[Serializable, NetSerializable]
public sealed partial class AirAlarmSetModePayload : NetworkPayload
{
    [DataField]
    public AirAlarmMode Mode;
}
