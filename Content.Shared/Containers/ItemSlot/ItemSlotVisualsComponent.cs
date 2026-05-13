using Content.Shared.Chemistry.Components;
using Robust.Shared.Serialization;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;

namespace Content.Shared.Containers.ItemSlot;

/// <summary>
///     The same concept as <see cref="SolutionContainerVisualsComponent"/> but now handles fill visuals per slot.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ItemSlotVisualsComponent : Component
{
    /// Like <see cref="ItemSlotsComponent"/> but for Visuals.
    [DataField(readOnly:true)]
    public Dictionary<string, ItemSlotVisuals> SlotVisuals = new();
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class ItemSlotVisuals
{
    [DataField]
    public ItemSlotVisualLayers Layer = ItemSlotVisualLayers.Fill;

    // Useful for specifying which Slot you want a Visual Layer for, not needed if the Item has one Slot.
    [DataField]
    public string? SlotName;

    [DataField]
    public string? FillBaseName;

    [DataField]
    public string? InHandsFillBaseName;

    [DataField]
    public string? EquippedFillBaseName;
}

[Serializable, NetSerializable]
public enum ItemSlotVisualLayers : byte
{
    Fill
}
