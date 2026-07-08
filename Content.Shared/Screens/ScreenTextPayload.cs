using Content.Shared.DeviceNetwork;

namespace Content.Shared.Screens;

public sealed partial class ScreenTextPayload : NetworkPayloadBase<ScreenTextPayload>
{
    [DataField]
    public string? Text;
}
