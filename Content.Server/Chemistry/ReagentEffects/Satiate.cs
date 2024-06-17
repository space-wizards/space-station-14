using Content.Shared.Chemistry.Reagent;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Default metabolism for drink reagents. Attempts to find a SatiationComponent on the target,
    /// and to update it's satiation values.
    /// </summary>
    public sealed partial class Satiate : ReagentEffect
    {
        private const float DefaultSatiationFactor = 3.0f;

        /// How much is satiated each metabolism tick. Not currently tied to
        /// rate or anything.
        [DataField("factor")]
        public float SatiationFactor { get; set; } = DefaultSatiationFactor;

        [DataField]
        public ProtoId<SatiationTypePrototype> SatiationType = "Hunger";

        /// Satiate thirst if a SatiationComponent can be found
        public override void Effect(ReagentEffectArgs args)
        {
            var uid = args.SolutionEntity;
            if (!args.EntityManager.TryGetComponent(uid, out SatiationComponent? component))
                return;

            args.EntityManager.System<SatiationSystem>().ModifySatiation((uid, component), SatiationType, SatiationFactor);
        }

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => Loc.GetString("reagent-effect-guidebook-satiate",
                    ("chance", Probability),
                    ("relative",  SatiationFactor / DefaultSatiationFactor),
                    ("type", Loc.GetString(prototype.Index(SatiationType).Name)));
    }
}
