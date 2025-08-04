using Robust.Shared.GameStates;

namespace Content.Shared.Overlays;

/// <summary>
/// Adds a rectangular shader when wearing an entity with this component.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ImageOverlayComponent : Component
{
    /// <summary>
    /// The alpha inside the rectangle.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public string PathToOverlayImage = "";

    /// <summary>
    /// The alpha inside the rectangle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AdditionalOverlayAlpha = 0f;

    /// <summary>
    /// Color of the shader being applied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color AdditionalColor = Color.Black;
}
