using Content.Shared.Hands.Components;

namespace Content.Client.Toggleable;

/// <summary>
///     Component that handles the toggling the visuals of some light emitting entity.
/// </summary>
/// <remarks>
///     This will toggle the visibility of layers on an entity's sprite, the in-hand visuals, and the clothing/equipment
///     visuals. This will modify the color of any attached point lights.
/// </remarks>
[RegisterComponent]
public sealed class ToggleableLightVisualsComponent : Component
{
    /// <summary>
    ///     Sprite layer that will have it's visibility toggled when this item is toggled.
    /// </summary>
    [DataField("spriteLayer")]
    public string SpriteLayer = "light";

    /// <summary>
    ///     Layers to add to the sprite of the player that is holding this entity (while the component is toggled on).
    /// </summary>
    [DataField("inhandVisuals")]
    public Dictionary<HandLocation, List<PrototypeLayerData>> InhandVisuals = new();

    /// <summary>
    ///     Layers to add to the sprite of the player that is wearing this entity (while the component is toggled on).
    /// </summary>
    [DataField("clothingVisuals")]
    public readonly Dictionary<string, List<PrototypeLayerData>> ClothingVisuals = new();
}
