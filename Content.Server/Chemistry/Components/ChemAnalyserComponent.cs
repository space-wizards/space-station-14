using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    /// <summary>
    /// For shared behavior between chemical analysis machines
    /// </summary>
    public sealed class ChemAnalyserComponent : Component
    {
        /// <summary>
        /// What method of input the machine will interact with
        /// </summary>
        [DataField("machineInputDevice", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>), required: false)]
        public string MachineInputDevice = string.Empty;

        [DataField("delay")]
        public float Delay = 5f;
        /// <summary>
        /// 
        /// </summary>
        [ViewVariables]
        [DataField("accumulator")]
        public float Accumulator = 0f;

        /// <summary>
        /// What the machine will spawn
        /// </summary>
        [DataField("machineOutput", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>), required: true)]
        public string MachineOutput = string.Empty;
    }
}
