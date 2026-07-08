namespace Content.Shared.DeviceNetwork.Payloads;

public sealed partial class ApcNetTogglePayload : NetworkPayloadBase<ApcNetTogglePayload>
{
    [DataField]
    public bool Enabled;
}
