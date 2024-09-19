using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;

namespace Content.Server.EntityEffects.Effects.PlantMetabolism;

public sealed partial class PlantAdjustMutationLevel : PlantAdjustAttribute
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-mutation-level";

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!CanMetabolize(args.TargetEntity, out PlantComponent? plantComp, args.EntityManager))
            return;

        plantComp.MutationLevel += Amount * plantComp.MutationMod;
    }
}
