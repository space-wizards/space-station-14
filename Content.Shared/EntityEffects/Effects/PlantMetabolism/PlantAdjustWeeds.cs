namespace Content.Shared.EntityEffects.Effects.PlantMetabolism;

public sealed partial class PlantAdjustWeeds : PlantAdjustAttribute<PlantAdjustWeeds>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-weeds";
    public override bool GuidebookIsAttributePositive { get; protected set; } = false;
}
