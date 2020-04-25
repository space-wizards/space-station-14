using System;
using System.Collections.Generic;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.BodySystem {



    /// <summary>
    ///     This data class represents the state of a BodyPart in regards to everything surgery related - whether there's an incision on it, whether the bone is broken, etc.
    /// </summary>	
    public abstract class ISurgeryData {
        public delegate void SurgeryAction(IEntity performer);

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
        public bool PerformSurgery(SurgeryToolType toolType, IEntity performer)
        {
            SurgeryAction step = GetSurgeryStep(toolType);
            if (step == null)
                return false;
            step(performer);
            return true;
        }

        public abstract bool CanRemoveMechanisms();
    }
}
