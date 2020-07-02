using Content.Server.Health.BodySystem.BodyParts;
using Content.Shared.BodySystem;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.Health.BodySystem.Surgery.SurgeryData
{
    /// <summary>
    ///     This data class represents the state of a BodyPart in regards to everything surgery related - whether there's an incision on it, whether the bone is broken, etc.
    /// </summary>
    public abstract class SurgeryData
    {
        /// <summary>
        ///     The BodyPart this surgeryData is attached to. The ISurgeryData class should not exist without a BodyPart that it represents, and will not work correctly without it.
        /// </summary>
        protected BodyPart Parent;

        /// <summary>
        ///     The BodyPartType of the parent PartType.
        /// </summary>
        protected BodyPartType ParentType => Parent.PartType;

        public delegate void SurgeryAction(BodyManagerComponent target, IEntity performer);

        public SurgeryData(BodyPart parent)
        {
            Parent = parent;
        }

        /// <summary>
        ///     Gets the delegate corresponding to the surgery step using the given SurgeryToolType. Returns null if no surgery step can be performed.
        /// </summary>
        public abstract SurgeryAction GetSurgeryStep(SurgeryToolType toolType);

        /// <summary>
        ///     Returns whether the given SurgeryToolType can be used to perform a surgery.
        /// </summary>
        public bool CheckSurgery(SurgeryToolType toolType)
        {
            return GetSurgeryStep(toolType) != null;
        }

        /// <summary>
        ///     Attempts to perform surgery with the given tooltype. Returns whether the operation was successful.
        /// </summary>
        /// /// <param name="toolType">The SurgeryToolType used for this surgery.</param>
        /// /// <param name="performer">The entity performing the surgery.</param>
        public bool PerformSurgery(SurgeryToolType toolType, BodyManagerComponent target, IEntity performer)
        {
            var step = GetSurgeryStep(toolType);
            if (step == null)
            {
                return false;
            }

            step(target, performer);
            return true;
        }

    }
}
