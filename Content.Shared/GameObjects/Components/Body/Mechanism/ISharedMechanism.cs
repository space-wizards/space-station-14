#nullable enable
using Content.Shared.GameObjects.Components.Body.Part;

namespace Content.Shared.GameObjects.Components.Body.Mechanism
{
    public interface ISharedMechanism : IHasBody
    {
        string Id { get; }

        string MechanismName { get; set; }

        /// <summary>
        ///     Professional description of the <see cref="ISharedMechanism"/>.
        /// </summary>
        string Description { get; set; }

        /// <summary>
        ///     The message to display upon examining a mob with this Mechanism installed.
        ///     If the string is empty (""), no message will be displayed.
        /// </summary>
        string ExamineMessage { get; set; }

        // TODO: Make RSI properties sane
        /// <summary>
        ///     Path to the RSI that represents this <see cref="ISharedMechanism"/>.
        /// </summary>
        string RSIPath { get; set; }

        /// <summary>
        ///     RSI state that represents this <see cref="ISharedMechanism"/>.
        /// </summary>
        string RSIState { get; set; }

        /// <summary>
        ///     Max HP of this <see cref="ISharedMechanism"/>.
        /// </summary>
        int MaxDurability { get; set; }

        /// <summary>
        ///     Current HP of this <see cref="ISharedMechanism"/>.
        /// </summary>
        int CurrentDurability { get; set; }

        /// <summary>
        ///     At what HP this <see cref="ISharedMechanism"/> is completely destroyed.
        /// </summary>
        int DestroyThreshold { get; set; }

        /// <summary>
        ///     Armor of this <see cref="ISharedMechanism"/> against attacks.
        /// </summary>
        int Resistance { get; set; }

        /// <summary>
        ///     Determines a handful of things - mostly whether this
        ///     <see cref="ISharedMechanism"/> can fit into a <see cref="ISharedBodyPart"/>.
        /// </summary>
        // TODO: OnSizeChanged
        int Size { get; set; }

        /// <summary>
        ///     What kind of <see cref="ISharedBodyPart"/> this
        ///     <see cref="ISharedMechanism"/> can be easily installed into.
        /// </summary>
        BodyPartCompatibility Compatibility { get; set; }

        ISharedBodyPart? Part { get; set; }

        void EnsureInitialize();

        void InstalledIntoBody();
    }
}
