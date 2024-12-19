using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;

namespace Content.Server.EntityEffects.Effects.PlantMetabolism;

[UsedImplicitly]
public sealed partial class PlantAdjustWeeds : PlantAdjustAttribute
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-weeds";
    public override bool GuidebookIsAttributePositive { get; protected set; } = false;

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!args.EntityManager.TryGetComponent(args.TargetEntity, out PlantHolderComponent? plantHolderComp))
            return;

        plantHolderComp.WeedLevel = Math.Clamp(plantHolderComp.WeedLevel + Amount, 0f, 10f);
    }
}
