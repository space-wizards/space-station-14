using Content.Server.Botany.Components;
using Content.Shared.EntityEffects;

namespace Content.Server.EntityEffects.Effects.PlantMetabolism;

/// <summary>
///     Handles increase or decrease of plant potency.
/// </summary>

public sealed partial class PlantAdjustPotency : PlantAdjustAttribute
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-potency";

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (!args.EntityManager.TryGetComponent(args.TargetEntity, out PlantComponent? plantComp)
            || plantComp.Dead || plantComp.Seed == null || plantComp.Seed.Immutable)
        {
            return;
        }

        plantComp.Seed.Potency = Math.Max(plantComp.Seed.Potency + Amount, 1);
    }
}
