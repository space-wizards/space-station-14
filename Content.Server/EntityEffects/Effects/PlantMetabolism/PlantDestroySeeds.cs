using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects.PlantMetabolism;

/// <summary>
///     Handles removal of seeds on a plant.
/// </summary>

public sealed partial class PlantDestroySeeds : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantHolderComp = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);
        if (plantHolderComp.PlantUid == null)
            return;
        var plantComp = args.EntityManager.GetComponent<PlantComponent>(plantHolderComp.PlantUid.Value);
        if (plantComp == null || plantComp.Dead || plantComp.Seed == null || plantComp.Seed.Immutable)
            return;

        if (plantComp.Seed != null && plantComp.Seed.Seedless == false)
        {
            var popupSystem = args.EntityManager.System<SharedPopupSystem>();
            popupSystem.PopupEntity(
                Loc.GetString("botany-plant-seedsdestroyed"),
                args.TargetEntity,
                PopupType.SmallCaution
            );
            plantComp.Seed.Seedless = true;
        }
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-plant-seeds-remove", ("chance", Probability));
}
