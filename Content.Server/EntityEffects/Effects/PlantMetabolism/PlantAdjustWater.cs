using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;

namespace Content.Server.EntityEffects.Effects.PlantMetabolism;

[UsedImplicitly]
public sealed partial class PlantAdjustWater : PlantAdjustAttribute
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-water";

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!CanMetabolize(args.TargetEntity, out var plantHolderComp, args.EntityManager, mustHaveAlivePlant: false))
            return;

        var plantHolder = args.EntityManager.System<PlantHolderSystem>();

        plantHolder.AdjustWater(args.TargetEntity, Amount, plantHolderComp);
    }
}

