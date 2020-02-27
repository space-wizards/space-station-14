using System;
using System.Collections.Generic;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.BodySystem {



    /// <summary>
    ///     This data class represents the state of a BodyPart in regards to surgery - whether there's an incision on it, whether the bone is broken, etc.
    /// </summary>	
    public interface ISurgeryData {

        /// <summary>
        ///     Attempts a surgery step with the given toolType data.
        /// </summary>
        public bool AttemptSurgery(SurgeryToolType toolType);
    }
}
