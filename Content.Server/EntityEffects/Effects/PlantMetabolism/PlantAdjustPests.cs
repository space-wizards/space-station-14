using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;

namespace Content.Server.EntityEffects.Effects.PlantMetabolism;

[UsedImplicitly]
public sealed partial class PlantAdjustPests : PlantAdjustAttribute
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-pests";
    public override bool GuidebookIsAttributePositive { get; protected set; } = false;

    public override void Effect(EntityEffectBaseArgs args)
    {
        var plantHolderComp = args.EntityManager.GetComponent<PlantHolderComponent>(args.TargetEntity);
        if (plantHolderComp.PlantUid == null || !CanMetabolize(plantHolderComp.PlantUid.Value, out PlantComponent? plantComp, args.EntityManager))
            return;

        plantHolderComp.PestLevel = Math.Clamp(plantHolderComp.PestLevel + Amount, 0, 10);
    }
}

