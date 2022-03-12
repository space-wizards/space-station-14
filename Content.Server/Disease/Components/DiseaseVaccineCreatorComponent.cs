using Content.Shared.Disease;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Disease.Components
{
    [RegisterComponent]
    /// For the swabs you use to take samples of diseases
    public class DiseaseVaccineCreatorComponent : Component
    {
        [DataField("delay")]
        public float Delay = 5f;

        /// <summary>
        /// How much time we've accumulated creating the vaccine
        /// </summary>
        [ViewVariables]
        public float Accumulator = 0f;

        /// <summary>
        /// What the machine will spawn
        /// </summary>
        [DataField("machineOutput", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string MachineOutput = "Vaccine";

        /// <summary>
        /// The disease prototype to add to the above
        /// </summary>
        [ViewVariables]
        public DiseasePrototype? Disease;
    }
}
