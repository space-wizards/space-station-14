using Content.Shared.Whitelist;

namespace Content.Server.SurveillanceCamera;

/// <summary>
///     Component that allows surveillance cameras to listen to the local
///     environment. All surveillance camera monitors have speakers for this.
/// </summary>
[RegisterComponent]
public sealed partial class SurveillanceCameraMicrophoneComponent : Component
{
    [DataField("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     Components that the microphone checks for to avoid transmitting
    ///     messages from these entities over the surveillance camera.
    ///     Used to avoid things like feedback loops, or radio spam.
    /// </summary>
    [DataField("blacklist")]
    public EntityWhitelist Blacklist { get; private set; } = new();

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("range")]
    public int Range { get; private set; } = 10;
}
