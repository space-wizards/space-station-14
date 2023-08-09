using Content.Shared.Disease;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Disease.Components
{
    [RegisterComponent]
    /// <summary>
    /// For shared behavior between both disease machines
    /// </summary>
    public sealed class DiseaseMachineComponent : Component
    {
        [DataField("delay")]
        public float Delay = 5f;
        /// <summary>
        /// How much time we've accumulated processing
        /// </summary>
        [DataField("accumulator")]
        public float Accumulator = 0f;
        /// <summary>
        /// The disease prototype currently being diagnosed
        /// </summary>
        [ViewVariables]
        public DiseasePrototype? Disease;
        /// <summary>
        /// What the machine will spawn
        /// </summary>
        [DataField("machineOutput", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>), required: true)]
        public string MachineOutput = string.Empty;
    }
}
