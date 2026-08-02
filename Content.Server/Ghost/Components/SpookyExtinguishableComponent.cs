using Content.Shared.Ghost;
using Robust.Shared.Audio;

namespace Content.Server.Ghost.Components;

/// <summary>
/// Causes an entity to react to ghost player using the "Boo!" action by
/// extinguishing the entity.
/// </summary>
/// <seealso cref="FlammableComponent"/>
/// <seealso cref="GhostBooEvent"/>
[RegisterComponent]
public sealed partial class SpookyExtinguishableComponent : Component
{
    /// <summary>
    /// The likelihood that a <see cref="GhostBooEvent"/> extinguishes this entity.
    /// </summary>
    [DataField]
    public float ExtinguishChance = 0.8f;

    /// <summary>
    /// The intensity of this response.
    /// </summary>
    /// <remarks>
    /// Extinguishing a candle, for example, is less distracting than a flickering light, so the default is Subtle.
    /// </remarks>
    [DataField]
    public GhostBooIntensity Intensity = GhostBooIntensity.Subtle;

    /// <summary>
    /// An optional sound that should play when this is extinguished.
    /// </summary>
    [DataField]
    public SoundSpecifier? ExtinguishSound = new SoundPathSpecifier("/Audio/Effects/quick_exhale.ogg");
}
