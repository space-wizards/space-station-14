#nullable enable
using Content.Shared.GameObjects.Components.Body.Part;

namespace Content.Shared.GameObjects.Components.Body.Mechanism
{
    public interface IMechanism : IHasBody
    {
        IBodyPart? Part { get; set; }

        /// <summary>
        ///     Professional description of the <see cref="IMechanism"/>.
        /// </summary>
        string Description { get; set; }

        /// <summary>
        ///     The message to display upon examining a mob with this
        ///     <see cref="IMechanism"/> added.
        ///     If the string is empty (""), no message will be displayed.
        /// </summary>
        string ExamineMessage { get; set; }

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

        // TODO BODY Turn these into event listeners so they dont need to be exposed
        /// <summary>
        ///     Called when the containing <see cref="IBodyPart"/> is attached to a
        ///     <see cref="IBody"/>.
        ///     For instance, attaching a head with a brain inside to a body.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        void AddedToBody();

        /// <summary>
        ///     Called when the parent <see cref="IMechanism"/> is
        ///     added into a <see cref="IBodyPart"/> that is not attached to a
        ///     <see cref="IBody"/>.
        ///     For instance, adding a brain to a dismembered head.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        void AddedToPart();

        /// <summary>
        ///     Called when the parent <see cref="IBodyPart"/> is removed from a
        ///     <see cref="IBody"/>.
        ///     For instance, removing a head with a brain inside from a body.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        void RemovedFromBody(IBody old);

        /// <summary>
        ///     Called when the parent <see cref="IMechanism"/> is
        ///     removed from a <see cref="IBodyPart"/> that is not attached to a
        ///     <see cref="IBody"/>.
        ///     For instance, removing a brain from a dismembered head.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        void RemovedFromPart(IBodyPart old);

        /// <summary>
        ///     Called when the parent <see cref="IMechanism"/> is added to a
        ///     <see cref="IBodyPart"/> that is attached to a <see cref="IBody"/>.
        ///     For instance, adding a brain to a head that is attached to a body.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        void AddedToPartInBody();

        /// <summary>
        ///     Called when the parent <see cref="IMechanism"/> is removed from a
        ///     <see cref="IBodyPart"/> that is attached to a <see cref="IBody"/>.
        ///     For instance, removing a brain from a head that is attached to a body.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY SYSTEM CODE!
        /// </summary>
        void RemovedFromPartInBody(IBody? oldBody, IBodyPart? oldPart);
    }
}
