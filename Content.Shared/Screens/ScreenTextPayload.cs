using Content.Shared.DeviceNetwork;

namespace Content.Shared.Screens;

/// <summary>
/// Broadcasts text to screens.
/// </summary>
public partial record struct ScreenTextPayload : INetworkPayload
{
    [DataField]
    public string? Text;
}
