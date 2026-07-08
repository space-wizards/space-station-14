using Content.Shared.DeviceNetwork;
using Robust.Shared.Serialization;

namespace Content.Shared.DeviceLinking;

[Serializable, NetSerializable]
public sealed partial class SignalPayload : NetworkPayloadBase<SignalPayload>
{
    [DataField]
    public string InvokedPort;

    [DataField]
    public INetworkPayload? Payload;
}
