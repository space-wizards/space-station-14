using System.Linq;
using Content.Shared.Disease;

namespace Content.Server.Disease.Components
{
    [RegisterComponent]
    /// <summary>
    /// Allows the enity to be infected with diseases.
    /// Please use only on mobs.
    /// </summary>
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
        /// <summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public List<DiseasePrototype> PastDiseases = new();
        /// <summary>
        /// All the diseases the carrier has or has had.
        /// Checked against when trying to add a disease
        /// <summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public List<DiseasePrototype> AllDiseases => PastDiseases.Concat(Diseases).ToList();
    }
}
