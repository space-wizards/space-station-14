using Content.Server.BodySystem;
using Content.Shared.BodySystem;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.Health.BodySystem.Surgery.Data
{
    /// <summary>
    ///     This data class represents the state of a <see cref="BodyPart"/> in regards to everything surgery related - whether there's an incision on it, whether the bone is broken, etc.
    /// </summary>
    public abstract class SurgeryData
    {
        /// <summary>
        ///     The <see cref="BodyPart"/> this surgeryData is attached to. The ISurgeryData class should not exist without a <see cref="BodyPart"/> that it
        ///     represents, and will throw errors if it is null.
        /// </summary>
        protected BodyPart _parent;

        /// <summary>
        ///     The <see cref="BodyPartType"/> of the parent <see cref="BodyPart"/>.
        /// </summary>
        protected BodyPartType _parentType => _parent.PartType;

        public delegate void SurgeryAction(IBodyPartContainer container, ISurgeon surgeon, IEntity performer);



        public SurgeryData(BodyPart parent)
        {
            _parent = parent;
        }

        /// <summary>
        ///     Returns the description of this current <see cref="BodyPart"/> to be shown upon observing the given entity.
        /// </summary>
        public abstract string GetDescription(IEntity target);

        /// <summary>
        ///     Returns whether a <see cref="Mechanism"/> can be installed into the <see cref="BodyPart"/> this ISurgeryData represents.
        /// </summary>
        public abstract bool CanInstallMechanism(Mechanism toBeInstalled);

        /// <summary>
        ///     Returns whether the given <see cref="BodyPart"/> can be connected to the <see cref="BodyPart"/> this ISurgeryData represents.
        /// </summary>
        public abstract bool CanAttachBodyPart(BodyPart toBeConnected);

        /// <summary>
        ///     Gets the delegate corresponding to the surgery step using the given <see cref="SurgeryType"/>. Returns null if no surgery step can be performed.
        /// </summary>
        public abstract SurgeryAction GetSurgeryStep(SurgeryType toolType);

        /// <summary>
        ///     Returns whether the given <see cref="SurgeryType"/> can be used to perform a surgery on the BodyPart this <see cref="SurgeryData"/> represents.
        /// </summary>
        public bool CheckSurgery(SurgeryType toolType)
        {
            return GetSurgeryStep(toolType) != null;
        }

        /// <summary>
        ///     Attempts to perform surgery of the given <see cref="SurgeryType"/>. Returns whether the operation was successful.
        /// </summary>
        /// <param name="surgeryType">The <see cref="SurgeryType"/> used for this surgery.</param>
        /// <param name="performer">The entity performing the surgery.</param>
        public bool PerformSurgery(SurgeryType surgeryType, IBodyPartContainer container, ISurgeon surgeon, IEntity performer)
        {
            SurgeryAction step = GetSurgeryStep(surgeryType);
            if (step == null)
                return false;
            step(container, surgeon, performer);
            return true;
        }

    }
}
