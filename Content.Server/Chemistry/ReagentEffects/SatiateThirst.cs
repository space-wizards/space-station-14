using Content.Server.Nutrition.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Nutrition.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Default metabolism for drink reagents. Attempts to find a ThirstComponent on the target,
    /// and to update it's thirst values.
    /// </summary>
    public sealed partial class SatiateThirst : ReagentEffect
    {
        private const float DefaultHydrationFactor = 3.0f;

        /// How much thirst is satiated each metabolism tick. Not currently tied to
        /// rate or anything.
        [DataField("factor")]
        public float HydrationFactor { get; set; } = DefaultHydrationFactor;

        /// Satiate thirst if a ThirstComponent can be found
        public override void Effect(ReagentEffectArgs args)
        {
            if (args.EntityManager.TryGetComponent(args.SolutionEntity, out ThirstComponent? thirst))
                EntitySystem.Get<ThirstSystem>().UpdateThirst(thirst, HydrationFactor);
        }

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-satiate-thirst", ("chance", Probability), ("relative",  HydrationFactor / DefaultHydrationFactor));
    }
}
