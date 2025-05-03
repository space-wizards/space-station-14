namespace Content.Shared.EntityEffects.Effects.PlantMetabolism;

public sealed partial class PlantAdjustHealth : PlantAdjustAttribute<PlantAdjustHealth>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-health";
}

