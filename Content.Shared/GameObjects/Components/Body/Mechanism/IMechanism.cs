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

        /// <summary>
        ///     Called when the part housing this mechanism is added to a body.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY PART CODE!
        /// </summary>
        /// <param name="old">The previous body, if any.</param>
        /// <param name="current">The new body.</param>
        void OnBodyAdd(IBody? old, IBody current);

        /// <summary>
        ///     Called when the part housing this mechanism is removed from
        ///     a body.
        ///     DO NOT CALL THIS DIRECTLY FROM OUTSIDE BODY PART CODE!
        /// </summary>
        /// <param name="old">The old body.</param>
        void OnBodyRemove(IBody old);
    }
}
