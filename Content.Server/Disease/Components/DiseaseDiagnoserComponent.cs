using Content.Shared.Disease;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Disease.Components
{
    [RegisterComponent]
    /// For the swabs you use to take samples of diseases
    public class DiseaseDiagnoserComponent : Component
    {
        [DataField("delay")]
        public float Delay = 5f;

        /// <summary>
        /// How much time we've accumulated printing.
        /// </summary>
        [ViewVariables]
        public float Accumulator = 0f;

        /// <summary>
        ///     The paper-type prototype to spawn with the order information.
        /// </summary>
        [DataField("printerOutput", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
        public string PrinterOutput = "Paper";

        /// <summary>
        /// The disease prototype currently being diagnosed
        /// </summary>
        [ViewVariables]
        public DiseasePrototype? Disease;
    }
}
