namespace Content.Server.Botany.Components;

[RegisterComponent]
[DataDefinition]
public sealed partial class WeedPestGrowthComponent : PlantGrowthComponent
{
    /// <summary>
    /// Maximum weed level the plant can tolerate before taking damage.
    /// </summary>
    [DataField]
    public float WeedTolerance = 5f;

    /// <summary>
    /// Maximum pest level the plant can tolerate before taking damage.
    /// </summary>
    [DataField]
    public float PestTolerance = 5f;

    /// <summary>
    /// Chance per tick for weeds to grow around this plant.
    /// </summary>
    [DataField]
    public float WeedGrowthChance = 0.01f;

    /// <summary>
    /// Amount of weed growth per successful weed growth tick.
    /// </summary>
    [DataField]
    public float WeedGrowthAmount = 0.5f;

    /// <summary>
    /// Weed level threshold at which the plant is considered overgrown and will transform into kudzu.
    /// </summary>
    [DataField]
    public float WeedHighLevelThreshold = 10f;

    /// <summary>
    /// Chance per tick for pests to damage this plant.
    /// </summary>
    [DataField]
    public float PestDamageChance = 0.05f;

    /// <summary>
    /// Amount of damage dealt to the plant per successful pest damage tick.
    /// </summary>
    [DataField]
    public float PestDamageAmount = 1f;
}
