namespace Content.Server.Magic;

[RegisterComponent]
public sealed partial class ChainFireballComponent : Component
{
    /// <summary>
    ///     The chance of the ball disappearing (in %)
    /// </summary>
    [DataField] public float DisappearChance = 0.05f;

    public List<EntityUid> IgnoredTargets = new();
}
