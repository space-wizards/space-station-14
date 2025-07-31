namespace Content.Server.Botany.Components;

[RegisterComponent]
public sealed partial class WeedPestGrowthComponent : PlantGrowthComponent
{
    [DataField]
    public float WeedTolerance = 5f;

    [DataField]
    public float PestTolerance = 5f;
}
