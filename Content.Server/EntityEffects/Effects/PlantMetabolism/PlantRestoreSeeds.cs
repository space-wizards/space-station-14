using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects.PlantMetabolism;

/// <summary>
///     Handles restoration of seeds on a plant.
/// </summary>

public sealed partial class PlantRestoreSeeds : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantHolderComp = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);
        if (plantHolderComp.PlantUid == null)
            return;
        var plantComp = args.EntityManager.GetComponent<PlantComponent>(plantHolderComp.PlantUid.Value);
        if (plantComp == null || plantComp.Dead || plantComp.Seed == null || plantComp.Seed.Immutable)
            return;

        if (plantComp.Seed.Seedless)
        {
            var popupSystem = args.EntityManager.System<SharedPopupSystem>();
            popupSystem.PopupEntity(Loc.GetString("botany-plant-seedsrestored"), args.TargetEntity);
            plantComp.Seed.Seedless = false;
        }
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-plant-seeds-add", ("chance", Probability));
}
