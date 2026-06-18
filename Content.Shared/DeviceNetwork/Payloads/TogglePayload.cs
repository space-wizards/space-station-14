namespace Content.Shared.DeviceNetwork.Payloads;

/// <summary>
/// Simple payload for toggling a device's state.
/// </summary>
public sealed partial class TogglePayload : NetworkPayload
{
    [DataField]
    public bool Enabled;
}
