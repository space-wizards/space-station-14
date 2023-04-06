namespace Content.Server.Kudzu;

[RegisterComponent]
public sealed class KudzuComponent : Component
{
    [DataField("spreadChance")]
    public float SpreadChance = 1f;
}
