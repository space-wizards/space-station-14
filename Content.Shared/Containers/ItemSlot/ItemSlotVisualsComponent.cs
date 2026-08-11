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
    [DataField]
    public ItemSlotVisualLayers Layer = ItemSlotVisualLayers.Fill;

    /// <summary>
    /// A string to specify which slot to use. Only useful if there's multiple ItemSlots, leave it empty for single slot items.
    /// </summary>
    [DataField]
    public string? SlotName;

    /// <summary>
    /// The name used for the Icon Fill.
    /// </summary>
    [DataField]
    public string? FillBaseName;

    [DataField]
    public string? InHandsFillBaseName;

    /// <summary>
    ///
    /// </summary>
    //[DataField]
    //public Dictionary<HandLocation, List<PrototypeLayerData>> InhandVisuals = new();

    [DataField]
    public string? EquippedFillBaseName;

    /// <summary>
    ///
    /// </summary>
    //[DataField]
    //public Dictionary<string, List<PrototypeLayerData>> ClothingVisuals = new();
}

[Serializable, NetSerializable]
public enum ItemSlotVisualLayers : byte
{
    Fill,
    Fill1,
    Fill2,
}
