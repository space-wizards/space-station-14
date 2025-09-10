namespace Content.Shared.EntityEffects.NewEffects.Botany.PlantAttributes;

/// <summary>
///     Handles increase or decrease of plant potency.
/// </summary>
public sealed partial class PlantAdjustPotency : BasePlantAdjustAttribute<PlantAdjustPotency>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-potency";
}
