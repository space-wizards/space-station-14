namespace Content.Shared.DeviceNetwork.Payloads;

/// <summary>
/// A payload for the Apc net switch.
/// </summary>
public partial record struct ApcNetTogglePayload : INetworkPayload
{
    [DataField]
    public bool Enabled;
}
