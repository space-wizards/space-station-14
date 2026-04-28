using Content.Shared.Chemistry.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Containers.ItemSlot;

/// <summary>
///     The same concept as <see cref="SolutionContainerVisualsComponent"/> but it now handles fill visuals as a binary
///     due to item slots allowing only one item per slot.
/// </summary>

// Only thing needed here is making it work with multiple visual layers so it can function with multiple slots &
// fully remove the need for ItemMapper for ItemSlots. That and it would make inhand/wielded/equipped gun visuals
// for inserting a magazine possible.
[RegisterComponent]
public sealed partial class ItemSlotVisualsComponent : Component
{
    [DataField]
    public string? FillBaseName;

    [DataField]
    public string? InHandsFillBaseName;

    [DataField]
    public string? EquippedFillBaseName;

    [DataField("layer")]
    public ItemSlotVisualLayers FillLayer = ItemSlotVisualLayers.Fill;
}

[Serializable, NetSerializable]
public enum ItemSlotVisualLayers : byte
{
    ContainsItem,
    Fill
}
