using Content.Shared.Disease;

namespace Content.Server.Disease
{
    [RegisterComponent]
    public sealed class DiseasedComponent : Component
    {
        public List<DiseasePrototype> Diseases = default!;
    }
}
