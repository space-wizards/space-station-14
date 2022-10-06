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
    [DataField("cellSlotId", required: true)]
    public string CellSlotId = string.Empty;

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
    /// Can this entity be inserted directly into a charging station? If false, you need to manually remove the power
    /// cell and recharge it separately.
    /// </summary>
    [DataField("fitsInCharger")]
    public bool FitsInCharger = true;

    public ItemSlot CellSlot { get; set; } = default!;
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
