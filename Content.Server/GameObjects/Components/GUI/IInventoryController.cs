using Robust.Shared.Interfaces.GameObjects;
using static Content.Shared.GameObjects.Components.Inventory.EquipmentSlotDefines;

namespace Content.Server.GameObjects
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
        /// <returns>True if the entity can be equipped, false otherwise</returns>
        bool CanEquip(Slots slot, IEntity entity, bool flagsCheck) => flagsCheck;
    }
}
