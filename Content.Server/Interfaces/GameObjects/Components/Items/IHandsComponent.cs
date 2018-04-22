using SS14.Shared.Interfaces.GameObjects;
using System.Collections.Generic;

namespace Content.Server.Interfaces.GameObjects
{
    public interface IHandsComponent : IComponent
    {
        /// <summary>
        ///     The hand index of the currently active hand.
        /// </summary>
        string ActiveIndex { get; set; }

        /// <summary>
        ///     Enumerates over every held item.
        /// </summary>
        IEnumerable<IItemComponent> GetAllHeldItems();

        /// <summary>
        ///     Gets the item held by a hand.
        /// </summary>
        /// <param name="index">The index of the hand to get.</param>
        /// <returns>The item in the held, null if no item is held</returns>
        IItemComponent GetHand(string index);

        /// <summary>
        /// Gets item held by the current active hand
        /// </summary>
        IItemComponent GetActiveHand { get; }

        /// <summary>
        ///     Puts an item into any empty hand, preferring the active hand.
        /// </summary>
        /// <param name="item">The item to put in a hand.</param>
        /// <returns>True if the item was inserted, false otherwise.</returns>
        bool PutInHand(IItemComponent item);

        /// <summary>
        ///     Puts an item into a specific hand.
        /// </summary>
        /// <param name="item">The item to put in the hand.</param>
        /// <param name="index">The index of the hand to put the item into.</param>
        /// <param name="fallback">
        ///     If true and the provided hand is full, the method will fall back to <see cref="PutInHand(IItemComponent)" />
        /// </param>
        /// <returns>True if the item was inserted into a hand, false otherwise.</returns>
        bool PutInHand(IItemComponent item, string index, bool fallback=true);

        /// <summary>
        ///     Checks to see if an item can be put in any hand.
        /// </summary>
        /// <param name="item">The item to check for.</param>
        /// <returns>True if the item can be inserted, false otherwise.</returns>
        bool CanPutInHand(IItemComponent item);

        /// <summary>
        ///     Checks to see if an item can be put in the specified hand.
        /// </summary>
        /// <param name="item">The item to check for.</param>
        /// <param name="index">The index for the hand to check for.</param>
        /// <returns>True if the item can be inserted, false otherwise.</returns>
        bool CanPutInHand(IItemComponent item, string index);

        /// <summary>
        ///     Drops an item on the ground, removing it from the hand.
        /// </summary>
        /// <param name="index">The hand to drop from.</param>
        /// <returns>True if an item was successfully dropped, false otherwise.</returns>
        bool Drop(string index);

        /// <summary>
        ///     Checks whether the item in the specified hand can be dropped.
        /// </summary>
        /// <param name="index">The hand to check for.</param>
        /// <returns>
        ///     True if the item can be dropped, false if the hand is empty or the item in the hand cannot be dropped.
        /// </returns>
        bool CanDrop(string index);

        /// <summary>
        ///     Adds a new hand to this hands component.
        /// </summary>
        /// <param name="index">The name of the hand to add.</param>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if a hand with specified name already exists.
        /// </exception>
        void AddHand(string index);

        /// <summary>
        ///     Removes a hand from this hands component.
        /// </summary>
        /// <remarks>
        ///     If the hand contains an item, the item is dropped.
        /// </remarks>
        /// <param name="index">The name of the hand to remove.</param>
        void RemoveHand(string index);

        /// <summary>
        ///     Checks whether a hand with the specified name exists.
        /// </summary>
        /// <param name="index">The hand name to check.</param>
        /// <returns>True if the hand exists, false otherwise.</returns>
        bool HasHand(string index);
    }
}
