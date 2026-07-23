using Robust.Client.GameObjects;
using Robust.Shared.Graphics.RSI;

namespace Content.Client.Sprite;

/// <summary>
/// Adds support for the sprite to override the sprite layer rendering order, based on which direction the sprite is facing.
/// <see cref="SpriteDirectionLayeringSystem.DirtyCachedOverrides"/> needs to be ran every time a new sprite layer is added/removed.
/// </summary>
[RegisterComponent, Access(typeof(SpriteDirectionLayeringSystem))]
public sealed partial class SpriteDirectionLayeringComponent : Component
{
    /// <summary>
    /// A dictionary of directions and layers; each direction has a list of which layer order should be done for that direction.
    /// </summary>
    /// <remarks>
    /// Note that these need to cover all parent/child layer groupings that may appear on a sprite, and may only render them once.
    /// Most likely these will match the entity's SpriteComponent layer mappings, and any further maps that are added during runtime
    /// are either included as parent/children, or as overlay sprites added to the end of the render list.
    /// </remarks>
    [DataField]
    public Dictionary<RsiDirection, List<PrototypeLayerData>> DirectionLayers = new();

    /// <summary>
    /// The direction types available in <see cref="DirectionLayers"/>, passed on to the sprite's <see cref="SpriteComponent.LayersOrderOverrideDirectionType"/>. By default 4, North/South/East/West.
    /// </summary>
    [DataField]
    public RsiDirectionType DirectionType = RsiDirectionType.Dir4;

    /// <summary>
    /// The layer overrides generated, each direction having a list of indexes to pass on to the sprite's <see cref="SpriteComponent.LayersOrderOverride"/>.
    /// </summary>
    [ViewVariables]
    public Dictionary<RsiDirection, List<int>> CachedLayerOverrides = new();

    /// <summary>
    /// If true, <see cref="CachedLayerOverrides"/> should be regenerated before rendering.
    /// </summary>
    [ViewVariables]
    public bool DirtyOverrides;
}
