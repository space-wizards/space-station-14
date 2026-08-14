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
    /// A Dictionary that handles multiple instances of ItemSlotVisuals. ItemSlotVisuals is for setting the Name, Layer,
    /// & sprite of an Icon/Inhand/Equipped Fill Sprites.
    /// </summary>
    [DataField]
    public Dictionary<string, ItemSlotVisuals> SlotVisuals = new();
}

[DataDefinition]
[Serializable, NetSerializable]
public partial struct ItemSlotVisuals()
{
    /// <summary>
    /// Enums from ItemSlotVisualLayers. Used to specify Visuals.
    /// </summary>
    [DataField]
    public ItemSlotVisualLayers Layer = ItemSlotVisualLayers.Fill;

    /// <summary>
    /// A string to specify which slot to use. Only useful if there's multiple ItemSlots or have multiple items that use
    /// the same slot, it'll default for single slot items when it's empty.
    /// </summary>
    [DataField]
    public string? SlotName = null;

    /// <summary>
    /// The name used for the Icon Fills.
    /// </summary>
    [DataField]
    public string? FillBaseName = null;

    /// <summary>
    /// The name used for the Inhand Fills.
    /// </summary>
    [DataField]
    public string? InHandsFillBaseName = null;

    /// <summary>
    /// The name used for the Back/Belt Fills.
    /// </summary>
    [DataField]
    public string? EquippedFillBaseName = null;
}

[Serializable, NetSerializable]
public enum ItemSlotVisualLayers : byte
{
    Fill,
    Fill1,
    Fill2,
}
