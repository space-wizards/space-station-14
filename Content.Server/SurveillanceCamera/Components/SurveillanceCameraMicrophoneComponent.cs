using Content.Shared.Whitelist;

namespace Content.Server.SurveillanceCamera;

/// <summary>
///     Component that allows surveillance cameras to listen to the local
///     environment. All surveillance camera monitors have speakers for this.
/// </summary>
[RegisterComponent]
public sealed partial class SurveillanceCameraMicrophoneComponent : Component
{
    [DataField]
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     Components that the microphone checks for to avoid transmitting
    ///     messages from these entities over the surveillance camera.
    ///     Used to avoid things like feedback loops, or radio spam.
    /// </summary>
    [DataField]
    public EntityWhitelist Blacklist { get; private set; } = new();

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public int Range { get; private set; } = 10;
}
