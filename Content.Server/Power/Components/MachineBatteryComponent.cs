using Content.Shared.Construction.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Power.Components
{

    [RegisterComponent]
    public sealed class MachineBatteryComponent : Component
    {
        /// <summary>
        ///     The machine part that affects the power capacity.
        /// </summary>
        [DataField("machinePartPowerCapacity", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
        public string MachinePartPowerCapacity = "Capacitor";

        /// <summary>
        ///     The machine part rating is raised to this power when calculating power gain
        /// </summary>
        [DataField("machinePartEfficiency")]
        public float MachinePartEfficiency = 1.5f;

        /// <summary>
        ///     Power gain scaling
        /// </summary>
        [DataField("powerCapacityGain")]
        public float PowerCapacityScaling = 8000000;
    }
}
