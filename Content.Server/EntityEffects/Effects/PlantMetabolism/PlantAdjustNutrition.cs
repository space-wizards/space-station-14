using Content.Server.Botany.Components;
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
         var plantComp = args.EntityManager.GetComponent<PlantComponent>(args.TargetEntity);
        var plantHolder = args.EntityManager.System<PlantHolderSystem>();
        if (plantComp.PlantHolderUid != null)
            plantHolder.AdjustNutrient(plantComp.PlantHolderUid.Value, Amount);
    }
}
