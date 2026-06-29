using Robust.Shared.GameStates;

namespace Content.Shared.Overlays;

/// <summary>
/// Enables the night-vision fullscreen overlay for the entity it is attached to.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NightVisionComponent : Component
{
    /// <summary>
    /// Overall color modulation applied on top of the night-vision shader output.
    /// </summary>
    [DataField]
    public Color Color = Color.DarkSlateGray;

    /// <summary>
    /// How much animated noise to add to the image (0..1).
    /// </summary>
    [DataField]
    public float NoiseAmount = 0.8f;

    /// <summary>
    /// Multiplier that scales the intensity of the noise added on top of the image.
    /// Higher values make the noise more pronounced.
    /// </summary>
    [DataField]
    public float NoiseMultiplier = 3.0f;
}
