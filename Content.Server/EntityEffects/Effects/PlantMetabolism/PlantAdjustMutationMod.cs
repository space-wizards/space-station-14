using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;

namespace Content.Server.EntityEffects.Effects.PlantMetabolism;

[UsedImplicitly]
public sealed partial class PlantAdjustMutationMod : PlantAdjustAttribute
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-mutation-mod";

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!CanMetabolize(args.TargetEntity, out PlantComponent? plantComp, args.EntityManager))
            return;

        plantComp.MutationMod = Math.Clamp(plantComp.MutationMod + Amount, 1f, 3f);
    }
}

