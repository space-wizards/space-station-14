namespace Content.Server.Traits.Assorted;

[RegisterComponent]
public sealed partial class KleptomaniacComponent : Component
{
    /// <summary>
    /// Chance to steal an item. Rolled once every <see cref="StealAttemptCooldown"/>.
    /// </summary>
    [DataField]
    public float StealChance = 0.1f;

    /// <summary>
    /// How long to wait between steal attempts. Regardless of success or failure,
    /// the kleptomaniac will wait this long before trying to steal again.
    /// </summary>
    [DataField]
    public TimeSpan StealAttemptCooldown = TimeSpan.FromMinutes(1);

    [DataField]
    public TimeSpan NextStealAttempt = TimeSpan.Zero;
}
