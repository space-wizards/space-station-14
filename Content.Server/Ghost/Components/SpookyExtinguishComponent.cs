using Robust.Shared.Audio;

namespace Content.Server.Ghost.Components;

/// <summary>
/// Causes this entity to react to ghost player using the "Boo!" action by
/// extinguishing its fire stacks if it has any.
/// </summary>
[RegisterComponent]
public sealed partial class SpookyExtinguishComponent : Component
{
    /// <summary>
    /// The likelihood that a ghost boo extinguishes this candle.
    /// </summary>
    [DataField]
    public float ExtinguishChance = 0.8f;

    /// <summary>
    /// The cost from the boo budget of extinguishing this.
    /// </summary>
    /// <remarks>
    /// Extinguishing a candle is less distracting than a flickering light, so the cost is very low.
    /// </remarks>
    [DataField]
    public int Cost = 1;

    /// <summary>
    /// An optional sound that should play when this is extinguished.
    /// </summary>
    [DataField]
    public SoundSpecifier? ExtinguishSound = new SoundPathSpecifier("/Audio/Effects/quick_exhale.ogg");
}
