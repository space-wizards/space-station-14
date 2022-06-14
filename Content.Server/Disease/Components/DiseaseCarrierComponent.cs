using System.Linq;
using Content.Shared.Disease;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Disease.Components
{
    /// <summary>
    /// Allows the entity to be infected with diseases.
    /// Please use only on mobs.
    /// </summary>
    [RegisterComponent]
    public sealed class DiseaseCarrierComponent : Component
    {
        /// <summary>
        /// Shows the CURRENT diseases on the carrier
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public List<DiseasePrototype> Diseases = new();

        /// <summary>
        /// The carrier's resistance to disease
        /// </summary>
        [DataField("diseaseResist")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float DiseaseResist = 0f;

        /// <summary>
        /// Diseases the carrier has had, used for immunity.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public List<DiseasePrototype> PastDiseases = new();

        /// <summary>
        /// All the diseases the carrier has or has had.
        /// Checked against when trying to add a disease
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public List<DiseasePrototype> AllDiseases => PastDiseases.Concat(Diseases).ToList();

        /// <summary>
        /// A list of diseases which the entity does not
        /// exhibit direct symptoms from. They still transmit
        /// these diseases, just without symptoms.
        /// </summary>
        [ViewVariables, DataField("carrierDiseases", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<DiseasePrototype>))]
        public HashSet<string>? CarrierDiseases;
    }
}
