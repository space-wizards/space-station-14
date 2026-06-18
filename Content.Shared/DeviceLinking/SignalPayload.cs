using Content.Shared.DeviceNetwork;

namespace Content.Shared.DeviceLinking;

public sealed partial class SignalPayload : NetworkPayload
{
    [DataField]
    public string InvokedPort;

    [DataField]
    public NetworkPayload? Payload;
}
