using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Toolshed.TypeParsers;

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


            var plantHolder = args.EntityManager.System<PlantHolderSystem>();
            var random = IoCManager.Resolve<IRobustRandom>();
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
