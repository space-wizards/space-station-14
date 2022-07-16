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
        /// <summary>
        ///     What stages this effect triggers on
        /// </summary>
        [DataField("stages")]
        public readonly int[] Stages = { 0 };
        /// <summary>
        /// What effect the disease will have.
        /// </summary>
        public abstract void Effect(DiseaseEffectArgs args);
    }
    /// <summary>
    /// What you have to work with in any disease effect/cure.
    /// Includes an entity manager because it is out of scope
    /// otherwise.
    /// </summary>
    public readonly record struct DiseaseEffectArgs(
        EntityUid DiseasedEntity,
        DiseasePrototype Disease,
        IEntityManager EntityManager
    );
}
