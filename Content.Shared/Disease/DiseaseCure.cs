using JetBrains.Annotations;

namespace Content.Shared.Disease
{
    [ImplicitDataDefinitionForInheritors]
    [MeansImplicitUse]
    public abstract class DiseaseCure
    {
        public abstract bool Cure(DiseaseEffectArgs args);
    }
}
