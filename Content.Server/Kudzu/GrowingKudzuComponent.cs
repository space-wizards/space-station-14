namespace Content.Server.Kudzu;

[RegisterComponent]
public sealed class GrowingKudzuComponent : Component
{
    [DataField("growthLevel")]
    public int GrowthLevel = 1;

    [DataField("growthTickSkipChance")]
    public float GrowthTickSkipChange = 0.0f;
}
