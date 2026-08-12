using Content.Shared.Chemistry.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.Components;
using Robust.Shared.Serialization;
using Robust.Shared.GameStates;

namespace Content.Shared.Containers.ItemSlot;

/// <summary>
///     The same concept as <see cref="SolutionContainerVisualsComponent"/> but now handles fill visuals per slot.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ItemSlotVisualsComponent : Component
{
    /// <summary>
    /// Like <see cref="ItemSlotsComponent"/> but for Visuals.
    /// </summary>
    [DataField(readOnly:true)]
    public Dictionary<string, ItemSlotVisuals> SlotVisuals = new();
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class ItemSlotVisuals
{
    /// <summary>
    /// The Layers in ItemSlotVisualLayers, can be used to show three visuals on icons/inhands/clothing.
    /// </summary>
    [DataField]
    public ItemSlotVisualLayers Layer = ItemSlotVisualLayers.Fill;

    /// <summary>
    /// A string to specify which slot to use. Only useful if there's multiple ItemSlots or have multiple items that use
    /// the same slot, it'll default for single slot items when it's empty.
    /// </summary>
    [DataField]
    public string? SlotName;

    /// <summary>
    /// The name used for the Icon Fills.
    /// </summary>
    [DataField]
    public string? FillBaseName;

    /// <summary>
    /// Layers to add to the sprite of the player that is holding this entity (while the component has an item inserted).
    /// Works in tandem with Layer to show multiple layers at once.
    /// </summary>
    [DataField]
    public Dictionary<HandLocation, List<PrototypeLayerData>> InhandVisuals = new();


    /// <summary>
    /// Works the same as the one in ToggleableVisualsComponent, but now it works in tandem with Layer to show
    /// multiple layers at once.
    /// </summary>
    [DataField]
    public Dictionary<string, List<PrototypeLayerData>> ClothingVisuals = new();
}

[Serializable, NetSerializable]
public enum ItemSlotVisualLayers : byte
{
    Fill,
    Fill1,
    Fill2,
}
