namespace Content.Shared.EntityEffects.Effects.PlantMetabolism;

public sealed partial class PlantAffectGrowth : PlantAdjustAttribute<PlantAffectGrowth>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-growth";
}

