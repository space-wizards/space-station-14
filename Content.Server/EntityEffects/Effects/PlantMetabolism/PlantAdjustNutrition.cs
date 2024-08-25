using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;

namespace Content.Server.EntityEffects.Effects.PlantMetabolism;

[UsedImplicitly]
public sealed partial class PlantAdjustNutrition : PlantAdjustAttribute
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-nutrition";

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!CanMetabolize(args.TargetEntity, out var plantHolderComp, args.EntityManager, mustHaveAlivePlant: false))
            return;

        var plantHolder = args.EntityManager.System<PlantHolderSystem>();

        plantHolder.AdjustNutrient(args.TargetEntity, Amount, plantHolderComp);
    }
}
