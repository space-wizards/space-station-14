using Robust.Shared.GameStates;

namespace Content.Shared.Overlays;

/// <summary>
/// Adds a rectangular shader when wearing an entity with this component.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WeldingMaskOverlayComponent : Component
{
    /// <summary>
    /// The alpha inside the rectangle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string PathToOverlayImage = "Textures/weldingTexture.png";

    /// <summary>
    /// The alpha inside the rectangle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AdditionalOverlayAlpha = 0.5f;

    /// <summary>
    /// Color of the shader being applied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color AdditionalColor = Color.Black;
}
