using JetBrains.Annotations;
using Robust.Shared.Utility;

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
        /// What effect the disease will have.
        /// </summary>
        public abstract void Effect(DiseaseEffectArgs args);

        /// <summary>
        /// What is the minimal severity needed for this effect to occur?
        /// </summary>
        [DataField("minSeverity")]
        public float MinSeverity
        {
            get => _minSeverity;
            set
            {
                DebugTools.Assert(value > 1f || value < 0f ,"MinSeverity have been attempted to be set out of range");
                _minSeverity = Math.Clamp(value, 0.0f, 1.0f);
            }
        }

        private float _minSeverity = 0.0f;

        /// <summary>
        /// What is the maximum severity that this effect can occur?
        /// </summary>
        [DataField("maxSeverity")]
        public float MaxSeverity
        {
            get => _maxSeverity;
            set
            {
                DebugTools.Assert(value > 1f || value < 0f ,"MaxSeverity have been attempted to be set out of range");
                _maxSeverity = Math.Clamp(value, 0.0f, 1.0f);
            }
        }

        private float _maxSeverity = 1.0f;
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
