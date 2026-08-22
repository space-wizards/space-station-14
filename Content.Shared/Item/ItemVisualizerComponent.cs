using Content.Shared.Hands.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Item;

/// <summary>
/// This is used for applying GenericVisualizer mappings to Inhand/Equipped visuals
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ItemVisualizerComponent : Component
{
    /// <summary>
    /// Layers to add to the sprite of the player that is holding this entity
    /// </summary>
    [DataField]
    public Dictionary<HandLocation, List<PrototypeLayerData>> InhandVisuals = new();

    /// <summary>
    /// Layers to add to the sprite of the player when they are wielding this entity
    /// </summary>
    [DataField]
    public Dictionary<HandLocation, List<PrototypeLayerData>> WieldedInhandVisuals = new();

    /// <summary>
    /// Layers to add to the sprite of the player when they are wearing this entity
    /// </summary>
    [DataField]
    public Dictionary<string, List<PrototypeLayerData>> ClothingVisuals = new();
}
