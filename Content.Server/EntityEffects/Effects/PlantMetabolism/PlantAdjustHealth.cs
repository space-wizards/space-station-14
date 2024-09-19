using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;

namespace Content.Server.EntityEffects.Effects.PlantMetabolism;

public sealed partial class PlantAdjustHealth : PlantAdjustAttribute
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-health";

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!CanMetabolize(args.TargetEntity, out PlantComponent? plantComp, args.EntityManager))
            return;

        plantComp.Health += Amount;
        if (plantComp.Health <= 0)
        {
            var plant = args.EntityManager.System<PlantSystem>();
            plant.Die(args.TargetEntity);
        }
    }
}

