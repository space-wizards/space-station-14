namespace Content.Server.Explosion.Components;

/// <summary>
///     Triggers when colliding with another entity.
/// </summary>
[RegisterComponent]
public sealed partial class TriggerOnCollideComponent : Component
{
    /// <summary>
    ///     The fixture with which to collide.
    /// </summary>
    [DataField(required: true)]
    public string FixtureID = string.Empty;

    /// <summary>
    ///     Doesn't trigger if the other colliding fixture is nonhard.
    /// </summary>
    [DataField]
    public bool IgnoreOtherNonHard = true;
}
