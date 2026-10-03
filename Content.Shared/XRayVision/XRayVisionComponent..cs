using Content.Shared.Actions;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.XRayVision;

/// <summary>
/// Enables the xray fullscreen overlay for the entity it is attached to or the wearer.
/// Shows tiles and whitelisted entities behind walls with a scanline shader.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class XRayVisionComponent : Component
{
    /// <summary>
    /// Whether the overlay should be visible.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    /// <summary>
    /// Whether wearing this entity should grant xray to the entity wearing it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RelayOverlay;

    /// <summary>
    /// The action proto that toggles the xray.
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
    /// Color of the scanline overlay applied to hidden tiles.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color TileOverlayColor = new(1f, 1f, 1f, 0.2f);

    /// <summary>
    /// Color of the scanline overlay applied to hidden entities.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color EntityOverlayColor = new(1f, 1f, 1f, 0.25f);

    /// <summary>
    /// Whether tiles behind walls should be shown with the scanline shader.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ShowTiles;

    /// <summary>
    /// Scanline effect intensity.
    /// The higher the intensity, the more visible the scanlines are.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ScanlinesIntensity;
}

public sealed partial class ToggleXRayVisionEvent : InstantActionEvent;
