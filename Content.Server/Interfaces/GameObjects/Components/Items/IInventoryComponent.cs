using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using System;

namespace Content.Server.Interfaces.GameObjects
{
    public interface IInventoryComponent : IComponent
    {
        /// <summary>
        ///     Gets the item in the specified slot.
        /// </summary>
        /// <param name="slot">The slot to get the item for.</param>
        /// <returns>Null if the slot is empty, otherwise the item.</returns>
        IItemComponent Get(string slot);

        /// <summary>
        ///     Gets the slot with specified name.
        ///     This gets the slot, NOT the item contained therein.
        /// </summary>
        /// <param name="slot">The name of the slot to get.</param>
        IInventorySlot GetSlot(string slot);

        /// <summary>
        ///     Puts an item in a slot.
        /// </summary>
        /// <remarks>
        ///     This will fail if there is already an item in the specified slot.
        /// </remarks>
        /// <param name="slot">The slot to put the item in.</param>
        /// <param name="item">The item to insert into the slot.</param>
        /// <returns>True if the item was successfully inserted, false otherwise.</returns>
        bool Insert(string slot, IItemComponent item);

        /// <summary>
        ///     Checks whether an item can be put in the specified slot.
        /// </summary>
        /// <param name="slot">The slot to check for.</param>
        /// <param name="item">The item to check for.</param>
        /// <returns>True if the item can be inserted into the specified slot.</returns>
        bool CanInsert(string slot, IItemComponent item);

        /// <summary>
        ///     Drops the item in a slot.
        /// </summary>
        /// <param name="slot">The slot to drop the item from.</param>
        /// <returns>True if an item was dropped, false otherwise.</returns>
        bool Drop(string slot);

        /// <summary>
        ///     Checks whether an item can be dropped from the specified slot.
        /// </summary>
        /// <param name="slot">The slot to check for.</param>
        /// <returns>
        ///     True if there is an item in the slot and it can be dropped, false otherwise.
        /// </returns>
        bool CanDrop(string slot);

        /// <summary>
        ///     Adds a new slot to this inventory component.
        /// </summary>
        /// <param name="slot">The name of the slot to add.</param>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if the slot with specified name already exists.
        /// </exception>
        IInventorySlot AddSlot(string slot);

        /// <summary>
        ///     Removes a slot from this inventory component.
        /// </summary>
        /// <remarks>
        ///     If the slot contains an item, the item is dropped.
        /// </remarks>
        /// <param name="slot">The name of the slot to remove.</param>
        void RemoveSlot(string slot);

        /// <summary>
        ///     Checks whether a slot with the specified name exists.
        /// </summary>
        /// <param name="slot">The slot name to check.</param>
        /// <returns>True if the slot exists, false otherwise.</returns>
        bool HasSlot(string slot);
    }

    public interface IInventorySlot
    {
        /// <summary>
        /// The name of the slot.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The item contained in the slot, can be null.
        /// </summary>
        IItemComponent Item { get; }

        /// <summary>
        /// The component owning us.
        /// </summary>
        IInventoryComponent Owner { get; }
    }
}
