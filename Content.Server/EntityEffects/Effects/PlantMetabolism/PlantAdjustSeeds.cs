using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Popups;

namespace Content.Server.EntityEffects.Effects.PlantMetabolism

{
    public sealed partial class PlantAdjustSeeds : PlantAdjustAttribute
    {

        public override string GuidebookAttributeName { get; set; } = "plant-attribute-seeds";

        public override void Effect(EntityEffectBaseArgs args)
        {
            if (!CanMetabolize(args.TargetEntity, out var plantHolderComp, args.EntityManager, mustHaveAlivePlant: false))
                return;

            if (plantHolderComp.Seed == null)
                return;

            var plantHolder = args.EntityManager.System<PlantHolderSystem>();
            var popupSystem = args.EntityManager.System<SharedPopupSystem>();

            if (Amount < 0) // If the amount is negative, destroy seeds
                if (plantHolderComp.Seed.Seedless == false)
                {
                    popupSystem.PopupEntity(Loc.GetString("botany-plant-seedsdestroyed"), args.TargetEntity, PopupType.SmallCaution);
                    plantHolderComp.Seed.Seedless = true;
                }

            if (Amount > 0) // If it's positive, restore them!
                if (plantHolderComp.Seed.Seedless)
                {
                    popupSystem.PopupEntity(Loc.GetString("botany-plant-seedsrestored"), args.TargetEntity);
                    plantHolderComp.Seed.Seedless = false;
                }




        }
    }
}
