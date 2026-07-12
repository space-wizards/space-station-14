using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.DeviceNetwork;

namespace Content.Server.Atmos.Monitor.Payloads;

/// <summary>
/// A network payload sent to an air alarm to set its mode.
/// </summary>
public sealed partial class AirAlarmSetModePayload : NetworkPayloadBase<AirAlarmSetModePayload>
{
    [DataField]
    public AirAlarmMode Mode;
}

/// <summary>
/// A network payload sent from an atmos device to an air alarm to update its UI.
/// </summary>
public sealed partial class AirAlarmSetDataPayload : NetworkPayloadBase<AirAlarmSetDataPayload>
{
    [DataField]
    public IAtmosDeviceData Payload;
}
