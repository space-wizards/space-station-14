namespace Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;

public sealed partial class PlantAdjustPests : BasePlantAdjustAttribute<PlantAdjustPests>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-pests";
    public override bool GuidebookIsAttributePositive { get; protected set; } = false;
}
