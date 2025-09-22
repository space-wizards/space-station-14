namespace Content.Shared.EntityEffects.Effects;

public sealed class ResetNarcolepsy : EntityEffectBase<ResetNarcolepsy>
{
    /// <summary>
    /// The time we set our narcolepsy timer to.
    /// </summary>
    [DataField("TimerReset")]
    public TimeSpan TimerReset = TimeSpan.FromSeconds(600);
}
