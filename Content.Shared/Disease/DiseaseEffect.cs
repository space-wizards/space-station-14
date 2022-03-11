using JetBrains.Annotations;

namespace Content.Shared.Disease
{
    [ImplicitDataDefinitionForInheritors]
    [MeansImplicitUse]
    public abstract class DiseaseEffect
    {
        /// <summary>
        ///     What's the chance, from 0 to 1, that this effect will occur?
        /// </summary>
        [DataField("probability")]
        public float Probability = 1.0f;
        public abstract void Effect(DiseaseEffectArgs args);
    }
    public readonly record struct DiseaseEffectArgs(
        EntityUid DiseasedEntity,
        DiseasePrototype Disease,
        IEntityManager EntityManager
    );
}
