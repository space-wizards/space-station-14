#nullable enable
using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Body.Behavior;
using Content.Shared.GameObjects.Components.Body.Part;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Body.Mechanism
{
    public interface IMechanism : IComponent
    {
        /// <summary>
        ///     The body that owns the <see cref="IBodyPart"/> in which this
        ///     <see cref="IMechanism"/> is in.
        /// </summary>
        IBody? Body { get; }

        /// <summary>
        ///     The part in which this <see cref="IMechanism"/> is in.
        /// </summary>
        IBodyPart? Part { get; set; }

        /// <summary>
        ///     The behaviors attached to this <see cref="IMechanism"/>
        ///     mapped by their type.
        /// </summary>
        IReadOnlyDictionary<Type, IMechanismBehavior> Behaviors { get; }

        /// <summary>
        ///     Max HP of this <see cref="IMechanism"/>.
        /// </summary>
        int MaxDurability { get; set; }

        /// <summary>
        ///     Current HP of this <see cref="IMechanism"/>.
        /// </summary>
        int CurrentDurability { get; set; }

        /// <summary>
        ///     At what HP this <see cref="IMechanism"/> is completely destroyed.
        /// </summary>
        int DestroyThreshold { get; set; }

        /// <summary>
        ///     Armor of this <see cref="IMechanism"/> against attacks.
        /// </summary>
        int Resistance { get; set; }

        /// <summary>
        ///     Determines a handful of things - mostly whether this
        ///     <see cref="IMechanism"/> can fit into a <see cref="IBodyPart"/>.
        /// </summary>
        // TODO BODY OnSizeChanged
        int Size { get; set; }

        /// <summary>
        ///     What kind of <see cref="IBodyPart"/> this
        ///     <see cref="IMechanism"/> can be easily installed into.
        /// </summary>
        BodyPartCompatibility Compatibility { get; set; }

        /// <summary>
        ///    Adds a <see cref="IMechanismBehavior"/> if this
        ///     <see cref="IMechanism"/> does not have it already.
        /// </summary>
        /// <typeparam name="T">The behavior type to add.</typeparam>
        /// <returns>
        ///     True if the behavior already existed, false if it had to be created.
        /// </returns>
        bool EnsureBehavior<T>(out T behavior) where T : IMechanismBehavior, new();

        /// <summary>
        ///     Checks if this <see cref="IMechanism"/> has the specified
        ///     <see cref="IMechanismBehavior"/>.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of <see cref="IMechanismBehavior"/> to check for.
        /// </typeparam>
        /// <returns>
        ///     true if it has the <see cref="IMechanismBehavior"/>, false otherwise.
        /// </returns>
        bool HasBehavior<T>() where T : IMechanismBehavior;

        /// <summary>
        ///     Removes the specified <see cref="IMechanismBehavior"/> from this
        ///     <see cref="IMechanism"/> if it has it.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of <see cref="IMechanismBehavior"/> to remove.
        /// </typeparam>
        /// <returns>true if it was removed, false otherwise.</returns>
        bool TryRemoveBehavior<T>() where T : IMechanismBehavior;

        /// <summary>
        ///     Runs an update cycle for this <see cref="IMechanism"/>.
        /// </summary>
        /// <param name="frameTime">
        ///     The amount of seconds that passed since the last update.
        /// </param>
        void Update(float frameTime);

        // TODO BODY Turn these into event listeners so they dont need to be exposed
        /// <summary>
        ///     Called when the containing <see cref="IBodyPart"/> is attached to a
        ///     <see cref="IBody"/>.
        ///     For instance, attaching a head with a brain inside to a body.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        /// <param name="body">
        ///     The body that this <see cref="IMechanism"/> was added to.
        /// </param>
        void AddedToBody(IBody body);

        /// <summary>
        ///     Called when the parent <see cref="IMechanism"/> is
        ///     added into a <see cref="IBodyPart"/> that is not attached to a
        ///     <see cref="IBody"/>.
        ///     For instance, adding a brain to a dismembered head.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        /// <param name="part">
        ///     The part that this <see cref="IMechanism"/> was added to.
        /// </param>
        void AddedToPart(IBodyPart part);

        /// <summary>
        ///     Called when the parent <see cref="IMechanism"/> is added to a
        ///     <see cref="IBodyPart"/> that is attached to a <see cref="IBody"/>.
        ///     For instance, adding a brain to a head that is attached to a body.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        /// <param name="body">
        ///     The body that this <see cref="IMechanism"/> was added to.
        /// </param>
        /// <param name="part">
        ///     The part that this <see cref="IMechanism"/> was added to.
        /// </param>
        void AddedToPartInBody(IBody body, IBodyPart part);

        /// <summary>
        ///     Called when the parent <see cref="IBodyPart"/> is removed from a
        ///     <see cref="IBody"/>.
        ///     For instance, removing a head with a brain inside from a body.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        /// <param name="old">
        ///     The body that this <see cref="IMechanism"/> was removed from.
        /// </param>
        void RemovedFromBody(IBody old);

        /// <summary>
        ///     Called when the parent <see cref="IMechanism"/> is
        ///     removed from a <see cref="IBodyPart"/> that is not attached to a
        ///     <see cref="IBody"/>.
        ///     For instance, removing a brain from a dismembered head.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        /// <param name="old">
        ///     The part that this <see cref="IMechanism"/> was removed from.
        /// </param>
        void RemovedFromPart(IBodyPart old);

        /// <summary>
        ///     Called when the parent <see cref="IMechanism"/> is removed from a
        ///     <see cref="IBodyPart"/> that is attached to a <see cref="IBody"/>.
        ///     For instance, removing a brain from a head that is attached to a body.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        /// <param name="oldBody">
        ///     The body that this <see cref="IMechanism"/> was removed from.
        /// </param>
        /// <param name="oldPart">
        ///     The part that this <see cref="IMechanism"/> was removed from.
        /// </param>
        void RemovedFromPartInBody(IBody oldBody, IBodyPart oldPart);
    }
}
