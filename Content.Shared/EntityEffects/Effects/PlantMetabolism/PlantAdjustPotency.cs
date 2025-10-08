// using Content.Server.Botany.Systems;

namespace Content.Shared.EntityEffects.Effects.PlantMetabolism;

/// <summary>
///     Handles increase or decrease of plant potency.
/// </summary>

public sealed partial class PlantAdjustPotency : PlantAdjustAttribute<PlantAdjustPotency>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-potency";
}
