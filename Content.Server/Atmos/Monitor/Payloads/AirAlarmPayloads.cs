using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.DeviceNetwork;

namespace Content.Server.Atmos.Monitor.Payloads;

/// <summary>
/// A network payload sent to an air alarm to set its mode.
/// </summary>
public partial record struct AirAlarmSetModePayload : INetworkPayload
{
    [DataField]
    public AirAlarmMode Mode;
}

/// <summary>
/// A network payload sent from an atmos device to an air alarm to update its UI.
/// </summary>
public partial record struct AirAlarmSetDataPayload : INetworkPayload
{
    [DataField]
    public IAtmosDeviceData Payload;
}
