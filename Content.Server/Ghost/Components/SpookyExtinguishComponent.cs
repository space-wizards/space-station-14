using Robust.Shared.Audio;

namespace Content.Server.Ghost.Components;

/// <summary>
/// Causes an entity to react to ghost player using the "Boo!" action by
/// extinguishing its fire stacks if it has any.
/// </summary>
/// <seealso cref="GhostBooEvent"/>
[RegisterComponent]
public sealed partial class SpookyExtinguishComponent : Component
{
    /// <summary>
    /// The likelihood that a <see cref="GhostBooEvent"/> extinguishes this entity.
    /// </summary>
    [DataField]
    public float ExtinguishChance = 0.8f;

    /// <summary>
    /// The cost from the boo budget of extinguishing this.
    /// </summary>
    /// <remarks>
    /// Extinguishing a candle, for example, is less distracting than a flickering light, so the default cost is very low.
    /// </remarks>
    [DataField]
    public int Cost = 1;

    /// <summary>
    /// An optional sound that should play when this is extinguished.
    /// </summary>
    [DataField]
    public SoundSpecifier? ExtinguishSound = new SoundPathSpecifier("/Audio/Effects/quick_exhale.ogg");
}
