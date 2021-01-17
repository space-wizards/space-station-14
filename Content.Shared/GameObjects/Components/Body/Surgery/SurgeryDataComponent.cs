#nullable enable
using Content.Shared.GameObjects.Components.Body.Mechanism;
using Content.Shared.GameObjects.Components.Body.Part;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects.Components.Body.Surgery
{
    /// <summary>
    ///     Represents the current surgery state of a <see cref="IBodyPart"/>.
    /// </summary>
    public interface ISurgeryData : IComponent
    {
        public delegate void SurgeryAction(IBodyPartContainer container, ISurgeon surgeon, IEntity performer);

        /// <summary>
        ///     The <see cref="IBodyPart"/> this
        ///     <see cref="ISurgeryData"/> is attached to.
        /// </summary>
        public IBodyPart? Parent { get; }

        /// <summary>
        ///     The <see cref="BodyPartType"/> of the parent
        ///     <see cref="IBodyPart"/>.
        /// </summary>
        public BodyPartType? ParentType { get; }

        /// <summary>
        ///     Returns a description of this entity.
        /// </summary>
        /// <returns>The description shown upon observing this entity.</returns>
        public string GetDescription();

        /// <summary>
        ///     Returns whether a <see cref="IMechanism"/> can be added into the
        ///     <see cref="IBodyPart"/> this <see cref="ISurgeryData"/>
        ///     represents.
        /// </summary>
        public bool CanAddMechanism(IMechanism mechanism);

        /// <summary>
        ///     Returns whether the given <see cref="IBodyPart"/> can be connected
        ///     to the <see cref="IBodyPart"/> this <see cref="ISurgeryData"/>
        ///     represents.
        /// </summary>
        public bool CanAttachBodyPart(IBodyPart part);

        /// <summary>
        ///     Gets the delegate corresponding to the surgery step using the given
        ///     <see cref="SurgeryType"/>.
        /// </summary>
        /// <returns>
        ///     The corresponding surgery action or null if no step can be
        ///     performed.
        /// </returns>
        public SurgeryAction? GetSurgeryStep(SurgeryType toolType);

        /// <summary>
        ///     Returns whether the given <see cref="SurgeryType"/> can be used to
        ///     perform a surgery on the <see cref="IBodyPart"/> this
        ///     <see cref="ISurgeryData"/> represents.
        /// </summary>
        public bool CheckSurgery(SurgeryType toolType)
        {
            return GetSurgeryStep(toolType) != null;
        }

        /// <summary>
        ///     Attempts to perform surgery of the given <see cref="SurgeryType"/>.
        /// </summary>
        /// <param name="surgeryType">
        ///     The <see cref="SurgeryType"/> used for this surgery.
        /// </param>
        /// <param name="container">
        ///     The container where the surgery is being done.
        /// </param>
        /// <param name="surgeon">
        ///     The entity being used to perform the surgery.
        /// </param>
        /// <param name="performer">The entity performing the surgery.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public bool PerformSurgery(SurgeryType surgeryType, IBodyPartContainer container, ISurgeon surgeon,
            IEntity performer);
    }
}
