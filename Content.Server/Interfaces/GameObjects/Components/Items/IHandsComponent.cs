using System;
using System.Collections.Generic;
using Content.Server.GameObjects.Components;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystemMessages;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Interfaces.GameObjects.Components.Items
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
        IEnumerable<ItemComponent> GetAllHeldItems();

        /// <summary>
        ///     Gets the item held by a hand.
        /// </summary>
        /// <param name="index">The index of the hand to get.</param>
        /// <returns>The item in the held, null if no item is held</returns>
        ItemComponent GetHand(string index);

        /// <summary>
        /// Gets item held by the current active hand
        /// </summary>
        ItemComponent GetActiveHand { get; }

        /// <summary>
        ///     Puts an item into any empty hand, preferring the active hand.
        /// </summary>
        /// <param name="item">The item to put in a hand.</param>
        /// <returns>True if the item was inserted, false otherwise.</returns>
        bool PutInHand(ItemComponent item);

        /// <summary>
        ///     Puts an item into a specific hand.
        /// </summary>
        /// <param name="item">The item to put in the hand.</param>
        /// <param name="index">The index of the hand to put the item into.</param>
        /// <param name="fallback">
        ///     If true and the provided hand is full, the method will fall back to <see cref="PutInHand(ItemComponent)" />
        /// </param>
        /// <returns>True if the item was inserted into a hand, false otherwise.</returns>
        bool PutInHand(ItemComponent item, string index, bool fallback=true);

        /// <summary>
        ///     Checks to see if an item can be put in any hand.
        /// </summary>
        /// <param name="item">The item to check for.</param>
        /// <returns>True if the item can be inserted, false otherwise.</returns>
        bool CanPutInHand(ItemComponent item);

        /// <summary>
        ///     Checks to see if an item can be put in the specified hand.
        /// </summary>
        /// <param name="item">The item to check for.</param>
        /// <param name="index">The index for the hand to check for.</param>
        /// <returns>True if the item can be inserted, false otherwise.</returns>
        bool CanPutInHand(ItemComponent item, string index);

        /// <summary>
        ///     Finds the hand slot holding the specified entity, if any.
        /// </summary>
        /// <param name="entity">
        ///     The entity to look for in our hands.
        /// </param>
        /// <returns>
        ///     The index of the hand slot if the entity is indeed held, <see langword="null" /> otherwise.
        /// </returns>
        string FindHand(IEntity entity);

        /// <summary>
        ///     Drops the item contained in the slot to the same position as our entity.
        /// </summary>
        /// <param name="slot">The slot of which to drop to drop the item.</param>
        /// <param name="doMobChecks">Whether to check the <see cref="ActionBlockerSystem.CanDrop()"/> for the mob or not.</param>
        /// <returns>True on success, false if something blocked the drop.</returns>
        bool Drop(string slot, bool doMobChecks = true);

        /// <summary>
        ///     Drops an item held by one of our hand slots to the same position as our owning entity.
        /// </summary>
        /// <param name="entity">The item to drop.</param>
        /// <param name="doMobChecks">Whether to check the <see cref="ActionBlockerSystem.CanDrop()"/> for the mob or not.</param>
        /// <returns>True on success, false if something blocked the drop.</returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown if <see cref="entity"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown if <see cref="entity"/> is not actually held in any hand.
        /// </exception>
        bool Drop(IEntity entity, bool doMobChecks = true);

        /// <summary>
        ///     Drops the item in a slot.
        /// </summary>
        /// <param name="slot">The slot to drop the item from.</param>
        /// <param name="coords"></param>
        /// <param name="doMobChecks">Whether to check the <see cref="ActionBlockerSystem.CanDrop()"/> for the mob or not.</param>
        /// <returns>True if an item was dropped, false otherwise.</returns>
        bool Drop(string slot, GridCoordinates coords, bool doMobChecks = true);

        /// <summary>
        ///     Drop the specified entity in our hands to a certain position.
        /// </summary>
        /// <remarks>
        ///     There are no checks whether or not the user is within interaction range of the drop location
        ///     or whether the drop location is occupied.
        /// </remarks>
        /// <param name="entity">The entity to drop, must be held in one of the hands.</param>
        /// <param name="coords">The coordinates to drop the entity at.</param>
        /// <param name="doMobChecks">Whether to check the <see cref="ActionBlockerSystem.CanDrop()"/> for the mob or not.</param>
        /// <returns>
        ///     True if the drop succeeded,
        ///     false if it failed (due to failing to eject from our hand slot, etc...)
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown if <see cref="entity"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown if <see cref="entity"/> is not actually held in any hand.
        /// </exception>
        bool Drop(IEntity entity, GridCoordinates coords, bool doMobChecks = true);

        /// <summary>
        ///     Drop the item contained in a slot into another container.
        /// </summary>
        /// <param name="slot">The slot of which to drop the entity.</param>
        /// <param name="targetContainer">The container to drop into.</param>
        /// <param name="doMobChecks">Whether to check the <see cref="ActionBlockerSystem.CanDrop()"/> for the mob or not.</param>
        /// <returns>True on success, false if something was blocked (insertion or removal).</returns>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if dry-run checks reported OK to remove and insert,
        ///     but practical remove or insert returned false anyways.
        ///     This is an edge-case that is currently unhandled.
        /// </exception>
        bool Drop(string slot, BaseContainer targetContainer, bool doMobChecks = true);

        /// <summary>
        ///     Drops an item in one of the hands into a container.
        /// </summary>
        /// <param name="entity">The item to drop.</param>
        /// <param name="targetContainer">The container to drop into.</param>
        /// <param name="doMobChecks">Whether to check the <see cref="ActionBlockerSystem.CanDrop()"/> for the mob or not.</param>
        /// <returns>True on success, false if something was blocked (insertion or removal).</returns>
        /// <exception cref="InvalidOperationException">
        ///     Thrown if dry-run checks reported OK to remove and insert,
        ///     but practical remove or insert returned false anyways.
        ///     This is an edge-case that is currently unhandled.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///     Thrown if <see cref="entity"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown if <see cref="entity"/> is not actually held in any hand.
        /// </exception>
        bool Drop(IEntity entity, BaseContainer targetContainer, bool doMobChecks = true);

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

        void HandleSlotModifiedMaybe(ContainerModifiedMessage message);
    }
}
