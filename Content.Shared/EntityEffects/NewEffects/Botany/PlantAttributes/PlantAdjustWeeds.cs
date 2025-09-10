namespace Content.Shared.EntityEffects.NewEffects.Botany.PlantAttributes;

public sealed partial class PlantAdjustWeeds : BasePlantAdjustAttribute<PlantAdjustWeeds>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-weeds";
    public override bool GuidebookIsAttributePositive { get; protected set; } = false;
}
