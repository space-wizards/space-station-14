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
        [ViewVariables(VVAccess.ReadWrite)]
        public List<DiseasePrototype> Diseases = new();

        [ViewVariables(VVAccess.ReadWrite)]
        public float DiseaseResist = 0f;
        [ViewVariables(VVAccess.ReadWrite)]
        public List<DiseasePrototype> PastDiseases = new();
        [ViewVariables(VVAccess.ReadWrite)]
        public List<DiseasePrototype> AllDiseases => PastDiseases.Concat(Diseases).ToList();
    }
}
