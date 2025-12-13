namespace Content.Server.Botany.Components;

/// <summary>
/// Damage tolerance of plant.
/// </summary>
[RegisterComponent]
[DataDefinition]
public sealed partial class UnviableGrowthComponent : Component
{
    /// <summary>
    /// Amount of damage dealt to the plant per successful tick with unviable.
    /// </summary>
    [DataField]
    public float UnviableDamage = 6f;
}
