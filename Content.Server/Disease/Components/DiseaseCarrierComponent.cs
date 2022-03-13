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
        [ViewVariables(VVAccess.ReadWrite)]
        /// <summary>
        /// The carrier's resistance to disease
        /// </summary>
        public float DiseaseResist = 0f;
        [ViewVariables(VVAccess.ReadWrite)]
        /// <summary>
        /// Diseases the carrier has had, used for immunity.
        /// <summary>
        public List<DiseasePrototype> PastDiseases = new();
        [ViewVariables(VVAccess.ReadWrite)]
        /// <summary>
        /// All the diseases the carrier has or has had.
        /// Checked against when trying to add a disease
        /// <summary>
        public List<DiseasePrototype> AllDiseases => PastDiseases.Concat(Diseases).ToList();
    }
}
