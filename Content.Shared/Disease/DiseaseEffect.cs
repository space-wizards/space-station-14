using JetBrains.Annotations;

namespace Content.Shared.Disease
{
    [ImplicitDataDefinitionForInheritors]
    [MeansImplicitUse]
    public abstract class DiseaseEffect
    {
        public abstract void Effect(DiseaseEffectArgs args);
    }
    public readonly record struct DiseaseEffectArgs(
        EntityUid DiseasedEntity,
        DiseasePrototype Disease
    );
}
