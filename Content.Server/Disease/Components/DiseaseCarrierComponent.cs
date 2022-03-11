using Content.Shared.Disease;

namespace Content.Server.Disease.Components
{
    [RegisterComponent]
    public sealed class DiseaseCarrierComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public List<DiseasePrototype> Diseases = new();

        [ViewVariables(VVAccess.ReadWrite)]
        public float DiseaseResist = 0f;
    }
}
