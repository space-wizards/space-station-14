using Content.Shared.EntityEffects;
using JetBrains.Annotations;

namespace Content.Server.EntityEffects.Effects.PlantMetabolism;

[UsedImplicitly]
public sealed partial class PlantAdjustToxins : PlantAdjustAttribute
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-toxins";
    public override bool GuidebookIsAttributePositive { get; protected set; } = false;

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!CanMetabolize(args.TargetEntity, out var plantHolderComp, args.EntityManager))
            return;

        plantHolderComp.Toxins += Amount;
    }
}

