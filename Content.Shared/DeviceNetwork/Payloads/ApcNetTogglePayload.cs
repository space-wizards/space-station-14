using Robust.Shared.Serialization;

namespace Content.Shared.DeviceNetwork.Payloads;

[Serializable, NetSerializable]
public sealed partial class ApcNetTogglePayload : NetworkPayload
{
    [DataField]
    public bool Enabled;
}
