using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;

namespace Content.Shared.PowerCell.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PowerCellSlotComponent : Component
{
    /// <summary>
    /// The actual item-slot that contains the cell. Allows all the interaction logic to be handled by <see cref="ItemSlotsSystem"/>.
    /// </summary>
    /// <remarks>
    /// Given that <see cref="PowerCellSystem"/> needs to verify that a given cell has the correct cell-size before
    /// inserting anyways, there is no need to specify a separate entity whitelist in this slot's yaml definition.
    /// </remarks>
    [DataField(required: true)]
    public string CellSlotId = string.Empty;

    /// <summary>
    /// Can this entity be inserted directly into a charging station? If false, you need to manually remove the power
    /// cell and recharge it separately.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool FitsInCharger = true;

}

