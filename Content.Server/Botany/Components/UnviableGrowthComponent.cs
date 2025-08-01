namespace Content.Server.Botany.Components;

[RegisterComponent]
[DataDefinition]
public sealed partial class UnviableGrowthComponent : PlantGrowthComponent
{
    /// <summary>
    /// Chance per tick for the plant to take damage due to being unviable.
    /// </summary>
    [DataField("deathChance")]
    public float DeathChance = 0.1f;

    /// <summary>
    /// Amount of damage dealt to the plant per successful death tick.
    /// </summary>
    [DataField("deathDamage")]
    public float DeathDamage = 6f;
}
