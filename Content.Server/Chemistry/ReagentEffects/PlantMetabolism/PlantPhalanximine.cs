using Content.Server.Botany.Components;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects.PlantMetabolism
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class PlantPhalanximine : ReagentEffect
    {
        public override void Effect(ReagentEffectArgs args)
        {
            if (!args.EntityManager.TryGetComponent(args.SolutionEntity, out PlantHolderComponent? plantHolderComp)
                                    || plantHolderComp.Seed == null || plantHolderComp.Dead ||
                                    plantHolderComp.Seed.Immutable)
                return;

            if (args.Source == null) return;

            var source = args.Source;
            //Need at least 5u, had to do it like this because plant metabolism is weird
            if (source.TryGetReagent("Phalanximine", out var quantity) && quantity >= 4.00)
            {
                plantHolderComp.Seed.Viable = true;
            }
        }

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => Loc.GetString("reagent-effect-guidebook-missing", ("chance", Probability));
    }
}
