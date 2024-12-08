namespace Content.Server.Botany.Components;

[RegisterComponent]
public sealed partial class BasicGrowthComponent : PlantGrowthComponent
{
    //Remaining TODOs/TO CHECKS
    //Update all of seeds.yml to have condensed component set? or set prototype to have them?
    //Ensure mutations work as expected for non-default growth components
    //ensure components / values are copied over when seeds are made/clipped.
    [DataField]
    public float WaterConsumption = 0.5f;

    [DataField]
    public float NutrientConsumption = 0.75f;
}
