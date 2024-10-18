using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;

namespace Content.Server.EntityEffects.Effects.PlantMetabolism;

public sealed partial class PlantAdjustMutationLevel : PlantAdjustAttribute
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-mutation-level";

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantHolderComp = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);
        if (plantHolderComp.PlantUid == null || !CanMetabolize(plantHolderComp.PlantUid.Value, out PlantComponent? plantComp, args.EntityManager))
            return;

        plantComp.MutationLevel += Amount * plantComp.MutationMod;
    }
}
