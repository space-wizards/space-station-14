using Content.Shared.DeviceNetwork;

namespace Content.Shared.Screens;

/// <summary>
/// Broadcasts text to screens.
/// </summary>
public sealed partial class ScreenTextPayload : NetworkPayloadBase<ScreenTextPayload>
{
    [DataField]
    public string? Text;
}
