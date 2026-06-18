using Content.Shared.DeviceNetwork;

namespace Content.Server.SensorMonitoring;

public sealed partial class BatterySensorSyncPayload : NetworkPayload
{
    [DataField]
    public BatterySensorData Data;
}

public sealed partial class BatterySensorRequestPayload : NetworkPayload;
