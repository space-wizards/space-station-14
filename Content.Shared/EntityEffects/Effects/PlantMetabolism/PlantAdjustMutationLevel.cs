namespace Content.Shared.EntityEffects.Effects.PlantMetabolism;

public sealed partial class PlantAdjustMutationLevel : PlantAdjustAttribute<PlantAdjustMutationLevel>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-mutation-level";
}
