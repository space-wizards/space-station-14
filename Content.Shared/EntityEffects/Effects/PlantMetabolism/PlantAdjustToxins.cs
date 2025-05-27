namespace Content.Shared.EntityEffects.Effects.PlantMetabolism;

public sealed partial class PlantAdjustToxins : PlantAdjustAttribute<PlantAdjustToxins>
{
    public override string GuidebookAttributeName { get; set; } = "plant-attribute-toxins";
    public override bool GuidebookIsAttributePositive { get; protected set; } = false;
}

