using Content.Shared.EntityEffects;
using JetBrains.Annotations;

namespace Content.Server.EntityEffects.Effects.PlantMetabolism;

[UsedImplicitly]
public sealed partial class PlantAdjustMutationMod : PlantAdjustAttribute
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-mutation-mod";

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!CanMetabolize(args.TargetEntity, out var plantHolderComp, args.EntityManager))
            return;

        plantHolderComp.MutationMod += Amount;
    }
}

