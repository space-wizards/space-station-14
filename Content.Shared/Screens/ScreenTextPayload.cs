using Content.Shared.DeviceNetwork;
using Robust.Shared.Serialization;

namespace Content.Shared.Screens;

[Serializable, NetSerializable]
public sealed partial class ScreenTextPayload : HandledNetworkPayload
{
    [DataField]
    public string? Text;
}
