using Content.Shared.Disease;

namespace Content.Server.Disease
{
    [RegisterComponent]
    public sealed class DiseaseCarrierComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public List<DiseasePrototype> Diseases = new();
    }
}
