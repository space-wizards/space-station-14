namespace Content.Server.Botany.Components;

[RegisterComponent]
[DataDefinition]
public sealed partial class UnviableGrowthComponent : PlantGrowthComponent
{
    [DataField("deathChance")]
    public float DeathChance = 0.1f;

    [DataField("deathDamage")]
    public float DeathDamage = 6f;
}
