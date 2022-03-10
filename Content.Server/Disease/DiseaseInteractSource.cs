using Content.Shared.Disease;

namespace Content.Server.Disease
{
    [RegisterComponent]
    public sealed class DiseaseInteractSourceComponent : Component
    {
        public List<DiseasePrototype> Diseases = default!;
    }
}
