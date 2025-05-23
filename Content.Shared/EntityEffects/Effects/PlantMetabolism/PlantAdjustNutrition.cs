namespace Content.Shared.EntityEffects.Effects.PlantMetabolism;

public sealed partial class PlantAdjustNutrition : PlantAdjustAttribute<PlantAdjustNutrition>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-nutrition";
}
