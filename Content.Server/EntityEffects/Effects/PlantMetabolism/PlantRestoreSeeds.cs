using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Content.Shared.Popups;

namespace Content.Server.EntityEffects.Effects.PlantMetabolism;

public sealed partial class PlantRestoreSeeds : EntityEffect
{
    public override void Effect(EntityEffectBaseArgs args)
    {
        if (
            !args.EntityManager.TryGetComponent(args.TargetEntity, out PlantHolderComponent? plantHolderComp)
            || plantHolderComp.Seed == null
            || plantHolderComp.Dead
            || plantHolderComp.Seed.Immutable
        )
            return;

        if (plantHolderComp.Seed == null)
            return;

        var plantHolder = args.EntityManager.System<PlantHolderSystem>();
        var popupSystem = args.EntityManager.System<SharedPopupSystem>();

        if (plantHolderComp.Seed.Seedless)
        {
            plantHolder.EnsureUniqueSeed(args.TargetEntity, plantHolderComp);
            popupSystem.PopupEntity(Loc.GetString("botany-plant-seedsrestored"), args.TargetEntity);
            plantHolderComp.Seed.Seedless = false;
        }
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
    Loc.GetString("reagent-effect-guidebook-plant-seeds-add", ("chance", Probability));
}


