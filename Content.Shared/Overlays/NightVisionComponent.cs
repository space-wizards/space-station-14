using Content.Shared.Actions;
using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Overlays;

/// <summary>
/// Enables the night-vision fullscreen overlay for the entity it is attached to or the wearer.
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
    /// Whether this night vision is prioritized.
    /// Causes it to overwrite all other sources of night vision, even if their noise is smaller.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Prioritized = true;

    /// <summary>
    /// Whether wearing this entity should grant night vision to the entity wearing it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RelayOverlay;

    /// <summary>
    /// The action proto that toggles the night vision.
    /// </summary>
    /// <remarks>
    /// if null, no action is added.
    /// if <see cref="RelayOverlay"/> is true. it adds the action to the entity wearing this.
    /// otherwise it adds the action to itself
    /// </remarks>
    [DataField]
    public EntProtoId? Action;

    /// <summary>
    /// Reference to the action entity
    /// </summary>
    [DataField]
    public EntityUid? ActionEntity;

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
    /// The color of the night vision phosphor that will be displayed as a monochromatic color to the user.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color PhosphorColor = new(0f, 1f, 0f, 1f);

    /// <summary>
    /// If the night vision circular goggle effect should be shown.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool GoggleEffect;

    /// <summary>
    /// How large the goggle radius should be, per circle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ViewCircleRadius;

    /// <summary>
    /// The spacing between goggle view circle centers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ViewCircleSpacing;

    /// <summary>
    /// The number of circles to render. Odd numbers of circles will start from the center.
    /// Recommended values between one and four.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ViewCircleCount;

    /// <summary>
    /// The amount of light multiplication the night vision system should apply.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Amplification = 32f;
}

public sealed partial class ToggleNightVisionEvent : InstantActionEvent;
