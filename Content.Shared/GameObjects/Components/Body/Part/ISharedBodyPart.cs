#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;

namespace Content.Shared.GameObjects.Components.Body.Part
{
    public interface ISharedBodyPart : IHasBody
    {
        /// <summary>
        ///     <see cref="BodyPartType"/> that this <see cref="ISharedBodyPart"/> is considered
        ///     to be.
        ///     For example, <see cref="BodyPartType.Arm"/>.
        /// </summary>
        BodyPartType PartType { get; }

        /// <summary>
        ///     The name of this <see cref="ISharedBodyPart"/>, often displayed to the user.
        ///     For example, it could be named "advanced robotic arm".
        /// </summary>
        public string PartName { get; }

        /// <summary>
        ///     Plural version of this <see cref="ISharedBodyPart"/> name.
        /// </summary>
        public string Plural { get; }

        /// <summary>
        ///     Determines many things: how many mechanisms can be fit inside this
        ///     <see cref="ISharedBodyPart"/>, whether a body can fit through tiny crevices,
        ///     etc.
        /// </summary>
        int Size { get; }

        /// <summary>
        ///     Max HP of this <see cref="ISharedBodyPart"/>.
        /// </summary>
        int MaxDurability { get; }

        /// <summary>
        ///     Current HP of this <see cref="ISharedBodyPart"/> based on sum of all damage types.
        /// </summary>
        int CurrentDurability { get; }

        /// <summary>
        ///     Collection of all <see cref="ISharedMechanism"/>s currently inside this
        ///     <see cref="ISharedBodyPart"/>.
        ///     To add and remove from this list see <see cref="AddMechanism"/> and
        ///     <see cref="RemoveMechanism"/>
        /// </summary>
        IReadOnlyCollection<ISharedMechanism> Mechanisms { get; }

        /// <summary>
        ///     Path to the RSI that represents this <see cref="ISharedBodyPart"/>.
        /// </summary>
        public string RSIPath { get; }

        /// <summary>
        ///     RSI state that represents this <see cref="ISharedBodyPart"/>.
        /// </summary>
        public string RSIState { get; }

        /// <summary>
        ///     RSI map keys that this body part changes on the sprite.
        /// </summary>
        public Enum? RSIMap { get; set; }

        /// <summary>
        ///     RSI color of this body part.
        /// </summary>
        // TODO: SpriteComponent rework
        public Color? RSIColor { get; set; }

        /// <summary>
        /// If body part is vital
        /// </summary>
        public bool IsVital { get; }

        bool SpawnDropped([NotNullWhen(true)] out IEntity? dropped);

        /// <summary>
        ///     Checks if the given <see cref="SurgeryType"/> can be used on
        ///     the current state of this <see cref="ISharedBodyPart"/>.
        /// </summary>
        /// <returns>True if it can be used, false otherwise.</returns>
        bool SurgeryCheck(SurgeryType surgery);

        /// <summary>
        ///     Checks if another <see cref="ISharedBodyPart"/> can be connected to this one.
        /// </summary>
        /// <param name="part">The part to connect.</param>
        /// <returns>True if it can be connected, false otherwise.</returns>
        bool CanAttachPart(ISharedBodyPart part);

        /// <summary>
        ///     Checks if a <see cref="IMechanism"/> can be installed on this
        ///     <see cref="ISharedBodyPart"/>.
        /// </summary>
        /// <returns>True if it can be installed, false otherwise.</returns>
        bool CanInstallMechanism(ISharedMechanism mechanism);

        /// <summary>
        ///     Tries to remove the given <see cref="ISharedMechanism"/> from
        ///     this <see cref="ISharedBodyPart"/>.
        /// </summary>
        /// <returns>
        ///     True if the mechanism was dropped, false otherwise.
        /// </returns>
        bool TryDropMechanism(IEntity dropLocation, ISharedMechanism mechanismTarget);

        /// <summary>
        ///     Tries to destroy the given <see cref="ISharedMechanism"/> from
        ///     this <see cref="ISharedBodyPart"/>.
        /// </summary>
        /// <returns>
        ///     True if the mechanism was destroyed, false otherwise.
        /// </returns>
        bool DestroyMechanism(ISharedMechanism mechanism);
    }
}
