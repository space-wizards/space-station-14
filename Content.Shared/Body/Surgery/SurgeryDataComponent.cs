using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Robust.Shared.GameObjects;

namespace Content.Shared.Body.Surgery
{
    /// <summary>
    ///     Represents the current surgery state of a <see cref="SharedBodyPartComponent"/>.
    /// </summary>
    public interface ISurgeryData : IComponent
    {
        public delegate void SurgeryAction(IBodyPartContainer container, ISurgeon surgeon, EntityUid performer);

        /// <summary>
        ///     The <see cref="SharedBodyPartComponent"/> this
        ///     <see cref="ISurgeryData"/> is attached to.
        /// </summary>
        public SharedBodyPartComponent? Parent { get; }

        /// <summary>
        ///     The <see cref="BodyPartType"/> of the parent
        ///     <see cref="SharedBodyPartComponent"/>.
        /// </summary>
        public BodyPartType? ParentType { get; }

        /// <summary>
        ///     Returns a description of this entity.
        /// </summary>
        /// <returns>The description shown upon observing this entity.</returns>
        public string GetDescription();

        /// <summary>
        ///     Returns whether a <see cref="SharedMechanismComponent"/> can be added into the
        ///     <see cref="SharedBodyPartComponent"/> this <see cref="ISurgeryData"/>
        ///     represents.
        /// </summary>
        public bool CanAddMechanism(SharedMechanismComponent mechanism);

        /// <summary>
        ///     Returns whether the given <see cref="SharedBodyPartComponent"/> can be connected
        ///     to the <see cref="SharedBodyPartComponent"/> this <see cref="ISurgeryData"/>
        ///     represents.
        /// </summary>
        public bool CanAttachBodyPart(SharedBodyPartComponent part);

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
        ///     perform a surgery on the <see cref="SharedBodyPartComponent"/> this
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
            EntityUid performer);
    }
}
