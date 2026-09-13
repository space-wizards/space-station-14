using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Sprite;

/// <summary>
/// Add a simple sprite overlay to an entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class SimpleSpriteOverlayComponent : Component
{
    /// <summary>
    /// Rsi of the sprite we want to overlay.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SpriteSpecifier OverlaySprite;

    /// <summary>
    /// The key of the entity layer into which the sprite will be inserted.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string LayerMap = "simple_visual_overlay";

    /// <summary>
    /// The shader (if any) that will be applied to the sprite layer.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? Shader;
}
