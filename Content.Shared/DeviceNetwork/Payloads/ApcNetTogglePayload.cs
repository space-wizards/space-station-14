namespace Content.Shared.DeviceNetwork.Payloads;

/// <summary>
/// A payload for the Apc net switch.
/// </summary>
public sealed partial class ApcNetTogglePayload : NetworkPayloadBase<ApcNetTogglePayload>
{
    [DataField]
    public bool Enabled;
}
