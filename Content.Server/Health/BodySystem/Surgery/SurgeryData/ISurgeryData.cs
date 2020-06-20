using System;
using System.Collections.Generic;
using Content.Shared.BodySystem;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Server.BodySystem
{



    /// <summary>
    ///     This data class represents the state of a BodyPart in regards to everything surgery related - whether there's an incision on it, whether the bone is broken, etc.
    /// </summary>	
    public abstract class ISurgeryData
    {

        /// <summary>
        ///     The BodyPart this surgeryData is attached to. The ISurgeryData class should not exist without a BodyPart that it represents, and will not work correctly without it (will throw errors if null).
        /// </summary>	
        protected BodyPart _parent;

        /// <summary>
        ///     The BodyPartType of the parent PartType.
        /// </summary>	
        protected BodyPartType _parentType => _parent.PartType;

        public delegate void SurgeryAction(IBodyPartContainer container, ISurgeon surgeon, IEntity performer);



        public ISurgeryData(BodyPart parent)
        {
            _parent = parent;
        }

        /// <summary>
        ///     Returns the description of this current limb to be shown upon observing the associated entity. 
        /// </summary>
        public abstract string GetDescription();

        /// <summary>
        ///     Returns whether a mechanism can be installed into the BodyPart this ISurgeryData represents. 
        /// </summary>
        public abstract bool CanInstallMechanism(Mechanism toBeInstalled);

        /// <summary>
        ///     Gets the delegate corresponding to the surgery step using the given <see cref="SurgeryType">SurgeryType</see>. Returns null if no surgery step can be performed.
        /// </summary>
        public abstract SurgeryAction GetSurgeryStep(SurgeryType toolType);

        /// <summary>
        ///     Returns whether the given <see cref="SurgeryType">SurgeryType</see> can be used to perform a surgery on the BodyPart this <see cref="ISurgeryData">ISurgeryData</see> represents.
        /// </summary>
        public bool CheckSurgery(SurgeryType toolType)
        {
            return GetSurgeryStep(toolType) != null;
        }

        /// <summary>
        ///     Attempts to perform surgery with the given tooltype. Returns whether the operation was successful.
        /// </summary>
        /// <param name="toolType">The <see cref="SurgeryType">SurgeryType</see> used for this surgery.</param>
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
