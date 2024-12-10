using Content.Server.Botany.Components;
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
        var plantHolder = args.EntityManager.System<PlantHolderSystem>();
        plantHolder.AdjustWater(args.TargetEntity, Amount);
    }
}

