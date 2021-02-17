#nullable enable
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Surgery;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Shared.GameObjects.Components.Body.Part
{
    public interface IBodyPart : IComponent, IBodyPartContainer
    {
        /// <summary>
        ///     The <see cref="IBody"/> to which this <see cref="IBodyPart"/> is
        ///     attached to.
        /// </summary>
        IBody? Body { get; set; }

        /// <summary>
        ///     <see cref="BodyPartType"/> that this <see cref="IBodyPart"/> is considered
        ///     to be.
        ///     For example, <see cref="BodyPartType.Arm"/>.
        /// </summary>
        BodyPartType PartType { get; }

        /// <summary>
        ///     Determines how many mechanisms can be fit inside this
        ///     <see cref="IBodyPart"/>.
        /// </summary>
        int Size { get; }

        // TODO BODY Mechanisms occupying different parts at the body level
        /// <summary>
        ///     Collection of all <see cref="IMechanism"/>s currently inside this
        ///     <see cref="IBodyPart"/>.
        ///     To add and remove from this list see <see cref="TryAddMechanism"/> and
        ///     <see cref="RemoveMechanism"/>
        /// </summary>
        IReadOnlyCollection<IMechanism> Mechanisms { get; }

        /// <summary>
        ///     Whether or not the owning <see cref="Body"/> will die if all
        ///     <see cref="IBodyPart"/>s of this type are removed from it.
        /// </summary>
        public bool IsVital { get; }

        /// <summary>
        ///     The symmetry of this <see cref="IBodyPart"/>.
        /// </summary>
        public BodyPartSymmetry Symmetry { get; }

        /// <summary>
        ///     Checks if the given <see cref="SurgeryType"/> can be used on
        ///     the current state of this <see cref="IBodyPart"/>.
        /// </summary>
        /// <returns>True if it can be used, false otherwise.</returns>
        bool SurgeryCheck(SurgeryType surgery);

        /// <summary>
        ///     Attempts to perform surgery on this <see cref="IBodyPart"/> with the given
        ///     tool.
        /// </summary>
        /// <returns>True if successful, false if there was an error.</returns>
        public bool AttemptSurgery(SurgeryType toolType, IBodyPartContainer target, ISurgeon surgeon,
            IEntity performer);

        /// <summary>
        ///     Checks if another <see cref="IBodyPart"/> can be connected to this one.
        /// </summary>
        /// <param name="part">The part to connect.</param>
        /// <returns>True if it can be connected, false otherwise.</returns>
        bool CanAttachPart(IBodyPart part);

        /// <summary>
        ///     Checks if a <see cref="IMechanism"/> can be added on this
        ///     <see cref="IBodyPart"/>.
        /// </summary>
        /// <returns>True if it can be added, false otherwise.</returns>
        bool CanAddMechanism(IMechanism mechanism);

        /// <summary>
        ///     Tries to add a <see cref="IMechanism"/> to this body.
        /// </summary>
        /// <param name="mechanism">The mechanism to add.</param>
        /// <param name="force">
        ///     Whether or not to check if the mechanism is compatible.
        ///     Passing true does not guarantee it to be added, for example if
        ///     it was already added before.
        /// </param>
        /// <returns>true if added, false otherwise even if it was already added.</returns>
        bool TryAddMechanism(IMechanism mechanism, bool force = false);

        /// <summary>
        ///     Tries to remove the given <see cref="mechanism"/> from this
        ///     <see cref="IBodyPart"/>.
        /// </summary>
        /// <param name="mechanism">The mechanism to remove.</param>
        /// <returns>True if it was removed, false otherwise.</returns>
        bool RemoveMechanism(IMechanism mechanism);

        /// <summary>
        ///     Tries to remove the given <see cref="mechanism"/> from this
        ///     <see cref="IBodyPart"/> and drops it at the specified coordinates.
        /// </summary>
        /// <param name="mechanism">The mechanism to remove.</param>
        /// <param name="dropAt">The coordinates to drop it at.</param>
        /// <returns>True if it was removed, false otherwise.</returns>
        bool RemoveMechanism(IMechanism mechanism, EntityCoordinates dropAt);

        /// <summary>
        ///     Tries to destroy the given <see cref="IMechanism"/> from
        ///     this <see cref="IBodyPart"/>.
        ///     The mechanism won't be deleted if it is not in this body part.
        /// </summary>
        /// <returns>
        ///     True if the mechanism was in this body part and destroyed,
        ///     false otherwise.
        /// </returns>
        bool DeleteMechanism(IMechanism mechanism);

        /// <summary>
        ///     Gibs the body part.
        /// </summary>
        void Gib();
    }
}
