using Content.Shared.Construction.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Bed.Components
{
    [RegisterComponent]
    public sealed class StasisBedComponent : Component
    {
        [DataField("baseMultiplier", required: true), ViewVariables(VVAccess.ReadWrite)]
        public float BaseMultiplier = 10f;

        /// <summary>
        /// What the metabolic update rate will be multiplied by (higher = slower metabolism)
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float Multiplier = 10f;

        [DataField("machinePartMetabolismModifier", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
        public string MachinePartMetabolismModifier = "Capacitor";
    }
}
