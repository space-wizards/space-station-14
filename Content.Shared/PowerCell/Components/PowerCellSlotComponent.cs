using System;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Shared.PowerCell.Components;

[RegisterComponent]
public sealed class PowerCellSlotComponent : Component
{
    public override string Name => "PowerCellSlot";

    /// <summary>
    /// What size of cell fits into this component.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("slotSize")]
    public PowerCellSize SlotSize { get; set; } = PowerCellSize.Small;

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
    public readonly string SlotName = "Power cell";

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
    /// String passed to <see><cref>String.Format</cref></see> when showing the description text for this item.
    /// String.Format is given a single parameter which is the size letter (S/M/L) of the cells this component uses.
    /// Use null to show no text.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("descFormatString")]
    public string? DescFormatString { get; set; } = "It uses size {0} power cells.";

}

public class PowerCellChangedEvent : EntityEventArgs
{
    public readonly bool Ejected;

    public PowerCellChangedEvent(bool ejected)
    {
        Ejected = ejected;
    }
}
