using Robust.Shared.Serialization;

namespace Content.Shared.DeviceNetwork.Payloads;

/// <summary>
/// Simple payload for toggling a device's state.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class TogglePayload : NetworkPayload
{
    [DataField]
    public bool Enabled;
}
