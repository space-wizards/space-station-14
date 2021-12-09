using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using static Content.Shared.Inventory.EquipmentSlotDefines;

namespace Content.Server.Inventory.Components
{
    /// <summary>
    ///     Allows for overriding inventory-related behavior on an entity.
    /// </summary>
    public interface IInventoryController
    {
        /// <summary>
        ///     Can be implemented to override "can this item be equipped" behavior.
        /// </summary>
        /// <param name="slot">The slot to be equipped into.</param>
        /// <param name="entity">The entity to equip.</param>
        /// <param name="flagsCheck">Whether the entity passes default slot masks & flags checks.</param>
        /// <param name="reason">The translated reason why the item cannot be equiped, if this function returns false. Can be null.</param>
        /// <returns>True if the entity can be equipped, false otherwise</returns>
        bool CanEquip(Slots slot, EntityUid entity, bool flagsCheck, [NotNullWhen(false)] out string? reason)
        {
            reason = null;
            return flagsCheck;
        }

        bool CanEquip(Slots slot, EntityUid entity, bool flagsCheck) => CanEquip(slot, entity, flagsCheck, out _);
    }
}
