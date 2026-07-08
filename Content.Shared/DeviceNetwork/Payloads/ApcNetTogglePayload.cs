using Robust.Shared.Serialization;

namespace Content.Shared.DeviceNetwork.Payloads;

[Serializable, NetSerializable]
public sealed partial class ApcNetTogglePayload : NetworkPayloadBase<ApcNetTogglePayload>
{
    [DataField]
    public bool Enabled;
}
