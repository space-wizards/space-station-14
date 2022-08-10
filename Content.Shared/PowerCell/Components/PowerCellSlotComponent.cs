using Content.Shared.Containers.ItemSlots;

namespace Content.Shared.PowerCell.Components;

[RegisterComponent]
public sealed class PowerCellSlotComponent : Component
{
    /// <summary>
    /// The actual item-slot that contains the cell. Allows all the interaction logic to be handled by <see cref="ItemSlotsSystem"/>.
    /// </summary>
    /// <remarks>
    /// Given that <see cref="PowerCellSystem"/> needs to verify that a given cell has the correct cell-size before
    /// inserting anyways, there is no need to specify a separate entity whitelist. In this slot's yaml definition.
    /// </remarks>
    [DataField("cellSlot")]
    public ItemSlot CellSlot = new();

    /// <summary>
    /// Name of the item-slot used to store cells. Determines the eject/insert verb text. E.g., "Eject > Power cell".
    /// </summary>
    /// <remarks>
    /// This is simply used provide a default value for <see cref="CellSlot.Name"/>. If this string is empty or
    /// whitespace, the verb will instead use the full name of any cell (e.g., "eject > small super-capacity power
    /// cell").
    /// </remarks>
    [DataField("slotName")]
    public readonly string SlotName = "power-cell-slot-component-slot-name-default"; // gets Loc.GetString()-ed by ItemSlotsSystem

    /// <summary>
    /// True if we don't want a cell inserted during map init. If a starting item is defined
    /// in the <see cref="CellSlot"/> yaml definition, that always takes precedence.
    /// </summary>
    /// <remarks>
    /// If false, the cell will start with a standard cell with a matching cell-size.
    /// </remarks>
    [DataField("startEmpty")]
    public bool StartEmpty = false;

    /// <summary>
    /// Descriptive text to add to add when examining an entity with a cell slot. If empty or whitespace, will not add
    /// any text.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("descFormatString")]
    public string? DescFormatString { get; set; } = "power-cell-slot-component-description-default";

    /// <summary>
    /// Can this entity be inserted directly into a charging station? If false, you need to manually remove the power
    /// cell and recharge it separately.
    /// </summary>
    [DataField("fitsInCharger")]
    public bool FitsInCharger = true;
}

/// <summary>
///     Raised directed at an entity with a power cell slot when the power cell inside has its charge updated or is ejected/inserted.
/// </summary>
public sealed class PowerCellChangedEvent : EntityEventArgs
{
    public readonly bool Ejected;

    public PowerCellChangedEvent(bool ejected)
    {
        Ejected = ejected;
    }
}
