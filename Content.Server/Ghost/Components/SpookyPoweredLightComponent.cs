using Content.Shared.Ghost.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Ghost.Components;

/// <summary>
/// Causes an entity to react to ghost player using the "Boo!" action by causing this light to flicker.
/// </summary>
/// <seealso cref="GhostBooEvent"/>
[RegisterComponent, AutoGenerateComponentPause, Access(typeof(GhostSystem))]
public sealed partial class SpookyPoweredLightComponent : Component
{
    /// <summary>
    /// The length of time the light should spend blinking.
    /// </summary>
    [DataField]
    public TimeSpan GhostBlinkingTime = TimeSpan.FromSeconds(10);

    /// <summary>
    /// The delay between when this light can start to blink.
    /// </summary>
    [DataField]
    public TimeSpan GhostBlinkingCooldown = TimeSpan.FromSeconds(60);

    /// <summary>
    /// The next time a ghost can cause this light to blink.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextGhostBlink;

    /// <summary>
    /// The intensity of this response to a <see cref="GhostBooEvent"/>.
    /// </summary>
    [DataField]
    public GhostBooIntensity Intensity = GhostBooIntensity.Normal;
}
