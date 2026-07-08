using Content.Shared.DeviceNetwork;

namespace Content.Shared.DeviceLinking;

public sealed partial class SignalPayload : NetworkPayloadBase<SignalPayload>
{
    [DataField]
    public string InvokedPort;

    [DataField]
    public INetworkPayload? Payload;
}
