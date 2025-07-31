namespace Content.Server.Botany.Components;

[RegisterComponent]
[DataDefinition]
public sealed partial class WeedPestGrowthComponent : PlantGrowthComponent
{
    [DataField("weedTolerance")]
    public float WeedTolerance = 5f;

    [DataField("pestTolerance")]
    public float PestTolerance = 5f;

    [DataField("weedGrowthChance")]
    public float WeedGrowthChance = 0.01f;

    [DataField("weedGrowthAmount")]
    public float WeedGrowthAmount = 0.5f;

    [DataField("pestDamageChance")]
    public float PestDamageChance = 0.05f;

    [DataField("pestDamageAmount")]
    public float PestDamageAmount = 1f;
}
