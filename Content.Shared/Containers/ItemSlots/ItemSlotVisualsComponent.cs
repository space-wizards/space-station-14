using Content.Shared.Chemistry.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Whitelist;
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
    /// Enums from ItemSlotVisualLayers, makes multiple visuals possible. Used to specify Visuals.
    /// </summary>
    [DataField]
    public ItemSlotVisualLayers Layer = ItemSlotVisualLayers.Fill0;

    /// <summary>
    /// A string to specify which slot to use from ItemSlots. Specifically the third string below the slots Dictionary.
    ///
    /// Useful if there's 2 or more tags of the same name but both are in different slots, or if you want to check
    /// if the slot has been filled in general, without specifying the tag. Checks if anything has been inserted by default.
    /// </summary>
    [DataField]
    public string? SlotName = null;

    /// <summary>
    /// A Whitelist to check if an item with the same tag/component has been inserted into the ItemSlot. Is made to be
    /// used with <see cref="ItemSlot"/> Whitelist.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

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
    Fill0,
    Fill1,
    Fill2,
}
