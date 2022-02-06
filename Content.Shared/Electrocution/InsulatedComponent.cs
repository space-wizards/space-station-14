using System;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Electrocution
{
    [Friend(typeof(SharedElectrocutionSystem))]
    [RegisterComponent, NetworkedComponent]
    public class InsulatedComponent : Component
    {
        /// <summary>
        ///     Siemens coefficient. Zero means completely insulated.
        /// </summary>
        [DataField("coefficient")]
        public float SiemensCoefficient { get; set; } = 0f;
    }

    // Technically, people could cheat and figure out which budget insulated gloves are gud and which ones are bad.
    // We might want to rethink this a little bit.
    [NetSerializable, Serializable]
    public class InsulatedComponentState : ComponentState
    {
        public float SiemensCoefficient { get; private set; }

        public InsulatedComponentState(float siemensCoefficient)
        {
            SiemensCoefficient = siemensCoefficient;
        }
    }
}
