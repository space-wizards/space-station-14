using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.Chemistry.Reagent;
using JetBrains.Annotations;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Chemistry.ReagentEffects.PlantMetabolism;

[UsedImplicitly]
[DataDefinition]
public sealed partial class PlantSedin : ReagentEffect
{
    [DataField]
    public float SeedRestorationChance = 0.1f;

    [DataField]
    public int PotencyDropAmount = 6; // Ideallly set to 2 times the amount Robust Harvest adds

    public override void Effect(ReagentEffectArgs args)
    {

        if (!args.EntityManager.TryGetComponent(args.SolutionEntity, out PlantHolderComponent? plantHolderComp)
                                || plantHolderComp.Seed == null || plantHolderComp.Dead ||
                                plantHolderComp.Seed.Immutable)
            return;


        var plantHolder = args.EntityManager.System<PlantHolderSystem>();
        var random = IoCManager.Resolve<IRobustRandom>();
        var popupSystem = args.EntityManager.System<SharedPopupSystem>();

        if (random.Prob(SeedRestorationChance) && plantHolderComp.Seed.Seedless)
        {
            popupSystem.PopupEntity(Loc.GetString("botany-plant-seedsrestored"), args.SolutionEntity);
            plantHolderComp.Seed.Seedless = false;
        }


        plantHolderComp.Seed.Potency = Math.Max(plantHolderComp.Seed.Potency - PotencyDropAmount, 1);

    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => Loc.GetString("reagent-effect-guidebook-missing", ("chance", Probability));
}
