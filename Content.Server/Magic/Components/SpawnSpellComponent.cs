namespace Content.Server.Magic;

/// <summary>
/// How long this summon will live for.
/// </summary>
[RegisterComponent]
public class SpawnSpellComponent : Component
{
    [DataField("lifetime")]
    public float Lifetime = 2f;
}
