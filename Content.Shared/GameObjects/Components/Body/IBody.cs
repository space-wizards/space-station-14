#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Body.Part.Property;
using Content.Shared.GameObjects.Components.Body.Preset;
using Content.Shared.GameObjects.Components.Body.Slot;
using Content.Shared.GameObjects.Components.Body.Template;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Body
{
    /// <summary>
    ///     Component representing a collection of <see cref="IBodyPart"/>s
    ///     attached to each other.
    /// </summary>
    public interface IBody : IComponent, IBodyPartContainer
    {
        /// <summary>
        ///     The <see cref="BodyTemplatePrototype"/> used to create this
        ///     <see cref="IBody"/>.
        /// </summary>
        public BodyTemplatePrototype? Template { get; }

        /// <summary>
        ///     The <see cref="BodyPresetPrototype"/> used to create this
        ///     <see cref="IBody"/>.
        /// </summary>
        public BodyPresetPrototype? Preset { get; }

        /// <summary>
        ///     An enumeration of the slots that make up this body, regardless
        ///     of if they contain a part or not.
        /// </summary>
        IEnumerable<BodyPartSlot> Slots { get; }

        /// <summary>
        ///     An enumeration of the parts on this body paired with the slots
        ///     that they are in.
        /// </summary>
        IEnumerable<KeyValuePair<IBodyPart, BodyPartSlot>> Parts { get; }

        /// <summary>
        ///     An enumeration of the slots on this body without a part in them.
        /// </summary>
        IEnumerable<BodyPartSlot> EmptySlots { get; }

        /// <summary>
        ///     Finds the central <see cref="BodyPartSlot"/>, if any,
        ///     of this body.
        /// </summary>
        /// <returns>
        ///     The central <see cref="BodyPartSlot"/> if one exists,
        ///     null otherwise.
        /// </returns>
        BodyPartSlot? CenterSlot { get; }

        /// <summary>
        ///     Finds the central <see cref="IBodyPart"/>, if any,
        ///     of this body.
        /// </summary>
        /// <returns>
        ///     The central <see cref="IBodyPart"/> if one exists,
        ///     null otherwise.
        /// </returns>
        IBodyPart? CenterPart { get; }

        // TODO BODY Sensible templates

        /// <summary>
        ///     Attempts to add a part to the given slot.
        /// </summary>
        /// <param name="slotId">The slot to add this part to.</param>
        /// <param name="part">The part to add.</param>
        /// <param name="checkSlotExists">
        ///     Whether to check if the slot exists, or create one otherwise.
        /// </param>
        /// <returns>
        ///     true if the part was added, false otherwise even if it was
        ///     already added.
        /// </returns>
        bool TryAddPart(string slotId, IBodyPart part);

        void SetPart(string slotId, IBodyPart part);

        /// <summary>
        ///     Checks if there is a <see cref="IBodyPart"/> in the given slot.
        /// </summary>
        /// <param name="slotId">The slot to look in.</param>
        /// <returns>
        ///     true if there is a part in the given <see cref="slotId"/>,
        ///     false otherwise.
        /// </returns>
        bool HasPart(string slotId);

        /// <summary>
        ///     Checks if this <see cref="IBody"/> contains the given <see cref="part"/>.
        /// </summary>
        /// <param name="part">The part to look for.</param>
        /// <returns>
        ///     true if the given <see cref="part"/> is attached to the body,
        ///     false otherwise.
        /// </returns>
        bool HasPart(IBodyPart part);

        /// <summary>
        ///     Removes the given <see cref="IBodyPart"/> from this body,
        ///     dropping other <see cref="IBodyPart"/> if they were hanging
        ///     off of it.
        /// <param name="part">The part to remove.</param>
        /// <returns>
        ///     true if the part was removed, false otherwise
        ///     even if the part was already removed previously.
        /// </returns>
        /// </summary>
        bool RemovePart(IBodyPart part);

        /// <summary>
        ///     Removes the body part in slot <see cref="slotId"/> from this body,
        ///     if one exists.
        /// </summary>
        /// <param name="slotId">The slot to remove it from.</param>
        /// <returns>true if the part was removed, false otherwise.</returns>
        bool RemovePart(string slotId);

        /// <summary>
        ///     Removes the body part from this body, if one exists.
        /// </summary>
        /// <param name="part">The part to remove from this body.</param>
        /// <param name="slotId">The slot that the part was in, if any.</param>
        /// <returns>
        ///     true if <see cref="part"/> was removed, false otherwise.
        /// </returns>
        bool RemovePart(IBodyPart part, [NotNullWhen(true)] out BodyPartSlot? slotId);

        /// <summary>
        ///     Disconnects the given <see cref="IBodyPart"/> reference, potentially
        ///     dropping other <see cref="IBodyPart">BodyParts</see> if they
        ///     were hanging off of it.
        /// </summary>
        /// <param name="slot">The part to drop.</param>
        /// <param name="dropped">
        ///     All of the parts that were dropped, including the one in
        ///     <see cref="slot"/>.
        /// </param>
        /// <returns>
        ///     true if the part was dropped, false otherwise.
        /// </returns>
        bool TryDropPart(BodyPartSlot slot, [NotNullWhen(true)] out Dictionary<BodyPartSlot, IBodyPart>? dropped);

        /// <summary>
        ///     Recursively searches for if <see cref="part"/> is connected to
        ///     the center.
        /// </summary>
        /// <param name="part">The body part to find the center for.</param>
        /// <returns>
        ///     true if it is connected to the center, false otherwise.
        /// </returns>
        bool ConnectedToCenter(IBodyPart part);

        /// <summary>
        ///     Returns whether the given part slot exists in this body.
        /// </summary>
        /// <param name="slot">The slot to check for.</param>
        /// <returns>true if the slot exists in this body, false otherwise.</returns>
        bool HasSlot(string slot);

        BodyPartSlot? GetSlot(IBodyPart part);

        /// <summary>
        ///     Finds the slot that the given <see cref="IBodyPart"/> resides in.
        /// </summary>
        /// <param name="part">
        ///     The <see cref="IBodyPart"/> to find the slot for.
        /// </param>
        /// <param name="slot">The slot found, if any.</param>
        /// <returns>true if a slot was found, false otherwise</returns>
        bool TryGetSlot(IBodyPart part, [NotNullWhen(true)] out BodyPartSlot? slot);

        /// <summary>
        ///     Finds the <see cref="IBodyPart"/> in the given
        ///     <see cref="slotId"/> if one exists.
        /// </summary>
        /// <param name="slotId">The part slot to search in.</param>
        /// <param name="result">The body part in that slot, if any.</param>
        /// <returns>true if found, false otherwise.</returns>
        bool TryGetPart(string slotId, [NotNullWhen(true)] out IBodyPart? result);

        /// <summary>
        ///     Checks if a slot of the specified type exists on this body.
        /// </summary>
        /// <param name="type">The type to check for.</param>
        /// <returns>true if present, false otherwise.</returns>
        bool HasSlotOfType(BodyPartType type);

        /// <summary>
        ///     Gets all slots of the specified type on this body.
        /// </summary>
        /// <param name="type">The type to check for.</param>
        /// <returns>An enumerable of the found slots.</returns>
        IEnumerable<BodyPartSlot> GetSlotsOfType(BodyPartType type);

        /// <summary>
        ///     Checks if a part of the specified type exists on this body.
        /// </summary>
        /// <param name="type">The type to check for.</param>
        /// <returns>true if present, false otherwise.</returns>
        bool HasPartOfType(BodyPartType type);

        /// <summary>
        ///     Gets all slots of the specified type on this body.
        /// </summary>
        /// <param name="type">The type to check for.</param>
        /// <returns>An enumerable of the found slots.</returns>
        IEnumerable<IBodyPart> GetPartsOfType(BodyPartType type);

        /// <summary>
        ///     Finds all <see cref="IBodyPart"/>s with the given property in
        ///     this body.
        /// </summary>
        /// <type name="type">The property type to look for.</type>
        /// <returns>A list of parts with that property.</returns>
        IEnumerable<(IBodyPart part, IBodyPartProperty property)> GetPartsWithProperty(Type type);

        /// <summary>
        ///     Finds all <see cref="IBodyPart"/>s with the given property in this body.
        /// </summary>
        /// <typeparam name="T">The property type to look for.</typeparam>
        /// <returns>A list of parts with that property.</returns>
        IEnumerable<(IBodyPart part, T property)> GetPartsWithProperty<T>() where T : class, IBodyPartProperty;

        // TODO BODY Make a slot object that makes sense to the human mind, and make it serializable. Imagine the possibilities!
        /// <summary>
        ///     Retrieves the slot at the given index.
        /// </summary>
        /// <param name="index">The index to look in.</param>
        /// <returns>A pair of the slot name and part type occupying it.</returns>
        BodyPartSlot SlotAt(int index);

        /// <summary>
        ///     Retrieves the part at the given index.
        /// </summary>
        /// <param name="index">The index to look in.</param>
        /// <returns>A pair of the part name and body part occupying it.</returns>
        KeyValuePair<IBodyPart, BodyPartSlot> PartAt(int index);

        /// <summary>
        ///     Gibs this body.
        /// </summary>
        /// <param name="gibParts">
        ///     Whether or not to also gib this body's parts.
        /// </param>
        void Gib(bool gibParts = false);
    }
}
