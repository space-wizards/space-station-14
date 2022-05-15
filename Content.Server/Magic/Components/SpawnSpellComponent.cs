namespace Content.Server.Magic;

/// <summary>
/// How long this summon will live for.
/// </summary>
[RegisterComponent]
public class SpawnSpellComponent : Component
{
    [ViewVariables]
    [DataField("lifetime")]
    public float Lifetime = 10f;
}
