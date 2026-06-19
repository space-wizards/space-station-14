using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.DeviceNetwork;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Monitor;

public sealed partial class AtmosRegisterDevicePayload : NetworkPayload;

public sealed partial class AtmosDeregisterDevicePayload : NetworkPayload;

public sealed partial class AtmosSyncDevicePayload : NetworkPayload
{
    [DataField]
    public AtmosDeviceDataPayload? Data;
}

public sealed partial class AtmosDeviceSetDataPayload : NetworkPayload
{
    [DataField]
    public AtmosDeviceDataPayload Data;
}

[ImplicitDataDefinitionForInheritors]
public abstract partial class AtmosAlarmableSourcePayload : NetworkPayload
{
    [DataField]
    public HashSet<ProtoId<TagPrototype>> Source = new();
}

public sealed partial class AtmosAlarmPayload : AtmosAlarmableSourcePayload
{
    [DataField]
    public AtmosAlarmType Type;

    [DataField]
    public AtmosMonitorThresholdTypeFlags TrippedThresholds;
}

public sealed partial class AtmosAlarmableSyncAlertsPayload : AtmosAlarmableSourcePayload
{
    [DataField]
    public Dictionary<string, AtmosAlarmType> AlarmStates = new();
}

public sealed partial class AtmosAlarmableResetAllPayload : AtmosAlarmableSourcePayload;

public sealed partial class AtmosMonitorSetThresholdPayload : NetworkPayload
{
    [DataField]
    public AtmosMonitorThresholdType Type;

    [DataField]
    public AtmosAlarmThreshold Threshold;

    [DataField]
    public Gas? Gas;
}

public sealed partial class AtmosMonitorSetAllThresholdsPayload : NetworkPayload
{
    [DataField]
    public AtmosSensorDataPayload Data;
}

public sealed partial class AirAlarmSetModePayload : NetworkPayload
{
    [DataField]
    public AirAlarmMode Mode;
}
