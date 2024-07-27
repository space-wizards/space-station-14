using Content.Server.Botany.Systems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Popups;

namespace Content.Server.Chemistry.ReagentEffects.PlantMetabolism
{
    public sealed partial class PlantAdjustSeeds : PlantAdjustAttribute
    {
        public override void Effect(ReagentEffectArgs args)
        {
            if (!CanMetabolize(args.SolutionEntity, out var plantHolderComp, args.EntityManager))
                return;

            if (plantHolderComp.Seed == null)
                return;

            var plantHolder = args.EntityManager.System<PlantHolderSystem>();
            var popupSystem = args.EntityManager.System<SharedPopupSystem>();

            if (Amount < 0) // If the amount is negative, destroy seeds
                if (plantHolderComp.Seed.Seedless == false)
                {
                    popupSystem.PopupEntity(Loc.GetString("botany-plant-seedsdestroyed"), args.SolutionEntity, PopupType.SmallCaution);
                    plantHolderComp.Seed.Seedless = true;
                }

            if (Amount > 0) // If it's positive, restore them!
                if (plantHolderComp.Seed.Seedless)
                {
                    popupSystem.PopupEntity(Loc.GetString("botany-plant-seedsrestored"), args.SolutionEntity);
                    plantHolderComp.Seed.Seedless = false;
                }




        }
    }
}
