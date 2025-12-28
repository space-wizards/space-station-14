using Robust.Shared.GameStates;

namespace Content.Shared.Overlays;

/// <summary>
/// Adds a rectangular shader when wearing an entity with this component.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WeldingMaskOverlayComponent : Component
{
    /// <summary>
    /// The width of the rectangle's bounds.
    /// 1.0 is at the edge of the game view.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float OuterRectangleWidth = 0.45f;

    /// <summary>
    /// The height of the rectangle's bounds.
    /// 1.0 is at the edge of the game view.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float OuterRectangleHeight = 0.4f;

    /// <summary>
    /// The thickness for the inner rectangle.
    /// Scaled on the y-axis. 0.5 goes to the middle of the screen.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float InnerRectangleThickness = 0.04f;

    /// <summary>
    /// The alpha outside the rectangle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float OuterAlpha = 1.0f;

    /// <summary>
    /// The alpha inside the rectangle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float InnerAlpha = 0.5f;

    /// <summary>
    /// Color of the shader being applied.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color Color = Color.Black;
}
