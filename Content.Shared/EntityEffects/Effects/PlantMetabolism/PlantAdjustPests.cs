namespace Content.Shared.EntityEffects.Effects.PlantMetabolism;

public sealed partial class PlantAdjustPests : PlantAdjustAttribute<PlantAdjustPests>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-pests";
    public override bool GuidebookIsAttributePositive { get; protected set; } = false;
}
