using SS14.Shared.Interfaces.GameObjects;

namespace Content.Server.Interfaces.GameObjects
{
    public interface IItemComponent : IComponent
    {
        /// <summary>
        ///     The inventory slot this item is stored in, if any.
        /// </summary>
        IInventorySlot ContainingSlot { get; }

        /// <summary>
        ///     Called when the item is removed from its inventory slot.
        /// </summary>
        void RemovedFromSlot();

        /// <summary>
        ///     Called when the item is inserted into a new inventory slot.
        /// </summary>
        void EquippedToSlot(IInventorySlot slot);
    }
}
