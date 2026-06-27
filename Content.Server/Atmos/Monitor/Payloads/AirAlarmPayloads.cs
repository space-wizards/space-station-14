using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.DeviceNetwork;
using Robust.Shared.Serialization;

namespace Content.Server.Atmos.Monitor.Payloads;

[Serializable, NetSerializable]
public sealed partial class AirAlarmSetModePayload : HandledNetworkPayload
{
    [DataField]
    public AirAlarmMode Mode;
}

[Serializable, NetSerializable]
public sealed partial class AirAlarmSetDataPayload : HandledNetworkPayload
{
    [DataField]
    public AtmosDeviceDataPayload Payload;
}
