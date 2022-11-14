using Content.Shared.Construction.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Bed.Components
{
    [RegisterComponent]
    public sealed class StasisBedComponent : Component
    {
        /// <summary>
        /// Stores whether or not the stasis bed has been emagged,
        /// which causes the multiplier to speed up rather than
        /// slow down. Needs to be stored for machine upgrades.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Emagged = false;

        [DataField("baseMultiplier", required: true), ViewVariables(VVAccess.ReadWrite)]
        public float BaseMultiplier = 10f;

        /// <summary>
        /// What the metabolic update rate will be multiplied by (higher = slower metabolism)
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float Multiplier = 10f;

        [DataField("machinePartMetabolismModifier", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
        public string MachinePartMetabolismModifier = "Manipulator";
    }
}
