using Content.Shared.Containers.ItemSlots;

namespace Content.Shared.PowerCell.Components;

[RegisterComponent]
public sealed partial class PowerCellSlotComponent : Component
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
