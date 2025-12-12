namespace Content.Server.Botany.Components;

/// <summary> Damage tolerance of plant. </summary>
[RegisterComponent]
[DataDefinition]
public sealed partial class UnviableGrowthComponent : Component
{
    /// <summary>
    /// Chance per tick for the plant to take damage due to being unviable.
    /// </summary>
    [DataField]
    public float DeathChance = 0.1f;

    /// <summary>
    /// Amount of damage dealt to the plant per successful death tick.
    /// </summary>
    [DataField]
    public float DeathDamage = 6f;
}
