namespace Content.Shared.Farming;

[RegisterComponent]
public sealed partial class CompostComponent : Component
{
    /// <summary>
    /// Amount of nutrition added to PlantHolder
    /// </summary>
    [DataField]
    public float NutritionValue = 50f;
}