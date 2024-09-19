using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;

namespace Content.Server.EntityEffects.Effects.PlantMetabolism;

[UsedImplicitly]
public sealed partial class PlantAffectGrowth : PlantAdjustAttribute
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-growth";

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!CanMetabolize(args.TargetEntity, out PlantComponent? plantComp, args.EntityManager))
            return;

        var plant = args.EntityManager.System<PlantSystem>();
        plant.AffectGrowth(args.TargetEntity, (int)Amount, plantComp);
    }
}

