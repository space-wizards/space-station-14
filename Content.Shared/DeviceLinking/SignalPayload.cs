using Content.Shared.DeviceNetwork;
using Robust.Shared.Serialization;

namespace Content.Shared.DeviceLinking;

[Serializable, NetSerializable]
public sealed partial class SignalPayload : NetworkPayload
{
    [DataField]
    public string InvokedPort;

    [DataField]
    public NetworkPayload? Payload;
}
