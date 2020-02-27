using System;
using System.Collections.Generic;
using Content.Shared.BodySystem;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.BodySystem {

    /// <summary>
    ///     Data class representing the surgery state of a biological entity.
    /// </summary>	
    [NetSerializable, Serializable]
    public class BiologicalSurgeryData : ISurgeryData {
        private bool _skinOpened = false;
        private bool _skinPulled = false;

        public bool AttemptSurgery(SurgeryToolType toolType)
        {
            return true;
            if (_skinOpened)
            {

            }
            if (_skinPulled)
            {

            }
        }
    }
}
