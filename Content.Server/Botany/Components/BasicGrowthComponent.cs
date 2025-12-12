namespace Content.Server.Botany.Components;

/// <summary>
/// Basic parameters for plant growth.
/// </summary>
[RegisterComponent]
[DataDefinition]
public sealed partial class BasicGrowthComponent : Component
{
    /// <summary>
    /// Amount of water consumed per growth tick.
    /// </summary>
    [DataField]
    public float WaterConsumption = 0.5f;

    /// <summary>
    /// Amount of nutrients consumed per growth tick.
    /// </summary>
    [DataField]
    public float NutrientConsumption = 0.75f;
}
