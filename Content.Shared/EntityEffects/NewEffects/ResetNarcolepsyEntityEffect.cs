namespace Content.Shared.EntityEffects.NewEffects;

public sealed class ResetNarcolepsy : EntityEffectBase<ResetNarcolepsy>
{
    /// <summary>
    /// The time we set our narcolepsy timer to.
    /// </summary>
    [DataField("TimerReset")]
    public TimeSpan TimerReset = TimeSpan.FromSeconds(600);
}
