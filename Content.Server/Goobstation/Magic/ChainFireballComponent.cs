namespace Content.Server.Magic;

[RegisterComponent]
public sealed partial class ChainFireballComponent : Component
{
    /// <summary>
    ///     The added chance of the ball disappearing (in %)
    /// </summary>
    [DataField] public float DisappearChanceDelta = 0.5f;

    /// <summary>
    ///     The chance of the ball disappearing (in %)
    /// </summary>
    [DataField] public float DisappearChance = 0f;

    public List<EntityUid> IgnoredTargets = new();
}
