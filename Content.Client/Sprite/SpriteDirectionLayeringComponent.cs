using Robust.Shared.Graphics.RSI;

namespace Content.Client.Sprite;

/// <summary>
/// Adds support for the sprite to override the sprite layer rendering order based on which direction the sprite is facing.
/// </summary>
[RegisterComponent, Access(typeof(SpriteDirectionLayeringSystem))]
public sealed partial class SpriteDirectionLayeringComponent : Component
{
    [DataField]
    public Dictionary<RsiDirection, List<PrototypeLayerData>> DirectionLayers = new();

    [ViewVariables]
    public RsiDirection? PreviousDirection;

    [ViewVariables]
    public Dictionary<RsiDirection, LinkedList<int>> CachedLayerOverrides = new();
}
