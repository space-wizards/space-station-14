using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.DeviceNetwork;

namespace Content.Server.Atmos.Monitor.Payloads;

public sealed partial class AirAlarmSetModePayload : HandledNetworkPayload
{
    [DataField]
    public AirAlarmMode Mode;
}

public sealed partial class AirAlarmSetDataPayload : HandledNetworkPayload
{
    [DataField]
    public AtmosDeviceDataPayload Payload;
}
