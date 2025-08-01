namespace Content.Server.Botany.Components;

[RegisterComponent]
[DataDefinition]
public sealed partial class WeedPestGrowthComponent : PlantGrowthComponent
{
    /// <summary>
    /// Maximum weed level the plant can tolerate before taking damage.
    /// </summary>
    [DataField("weedTolerance")]
    public float WeedTolerance = 5f;

    /// <summary>
    /// Maximum pest level the plant can tolerate before taking damage.
    /// </summary>
    [DataField("pestTolerance")]
    public float PestTolerance = 5f;

    /// <summary>
    /// Chance per tick for weeds to grow around this plant.
    /// </summary>
    [DataField("weedGrowthChance")]
    public float WeedGrowthChance = 0.01f;

    /// <summary>
    /// Amount of weed growth per successful weed growth tick.
    /// </summary>
    [DataField("weedGrowthAmount")]
    public float WeedGrowthAmount = 0.5f;

    /// <summary>
    /// Chance per tick for pests to damage this plant.
    /// </summary>
    [DataField("pestDamageChance")]
    public float PestDamageChance = 0.05f;

    /// <summary>
    /// Amount of damage dealt to the plant per successful pest damage tick.
    /// </summary>
    [DataField("pestDamageAmount")]
    public float PestDamageAmount = 1f;
}
