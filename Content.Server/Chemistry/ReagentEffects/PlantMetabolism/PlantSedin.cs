using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Content.Server.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Chemistry.ReagentEffects.PlantMetabolism
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class PlantSedin : ReagentEffect
    {

        [Dependency] private readonly PopupSystem _popupSystem = default!;


        [DataField]
        public float SeedRestorationChance = 0.1f;

        [DataField]
        public int PotencyDropAmount = 7; // Nerf as neccesary

        public override void Effect(ReagentEffectArgs args)
        {
            if (!args.EntityManager.TryGetComponent(args.SolutionEntity, out PlantHolderComponent? plantHolderComp)
                                    || plantHolderComp.Seed == null || plantHolderComp.Dead ||
                                    plantHolderComp.Seed.Immutable)
                return;


            var plantHolder = args.EntityManager.System<PlantHolderSystem>();
            var random = IoCManager.Resolve<IRobustRandom>();

            if (random.Prob(SeedRestorationChance))
            {
                // _popupSystem.PopupEntity(Loc.GetString("botany-plant-seedsrestored"), plantHolder., plantHolderComp.Owner); // Dunno how to do this, I'll figure it out!
                plantHolderComp.Seed.Seedless = false;
            }


            plantHolderComp.Seed.Potency = Math.Max(plantHolderComp.Seed.Potency - PotencyDropAmount, 1);

        }

        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => Loc.GetString("reagent-effect-guidebook-missing", ("chance", Probability));
    }
}
