using Robust.Shared.GameStates;

namespace Content.Shared.Overlays;

/// <summary>
/// Enables the night-vision fullscreen overlay for the entity it is attached to.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class NightVisionComponent : Component
{
    /// <summary>
    /// Whether the overlay should be visible.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// Overall color modulation applied on top of the night-vision screen shader.
    /// Does not control lighting coloring, just serves as an effect on the screen.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color OverlayColor = Color.Transparent; // Transparent by default, no overlay.

    /// <summary>
    /// Color modification added on top of lighting during rendering.
    /// This is the part responsible for making things bright.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color LightingColor = new(1f, 1f, 1f, 0.15f);

    /// <summary>
    /// How much animated noise to add to the image (0..1).
    /// </summary>
    [DataField, AutoNetworkedField]
    public float NoiseAmount;

    /// <summary>
    /// Multiplier that scales the intensity of the noise added on top of the image.
    /// Higher values make the noise more pronounced.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float NoiseMultiplier;
}
