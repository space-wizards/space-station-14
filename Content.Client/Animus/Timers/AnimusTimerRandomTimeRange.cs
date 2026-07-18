using Robust.Client.Animus.Timers;

namespace Content.Client.Animus.Timers;

public sealed partial class AnimusTimerRandomTimeRange : AnimusTimerBase
{
    /// <summary>
    /// The minimum for the next period in seconds.
    /// </summary>
    [DataField]
    public TimeSpan MinTime;

    /// <summary>
    /// The maximum time for the next period in seconds.
    /// </summary>
    [DataField]
    public TimeSpan MaxTime;

    public override TimeSpan GetNextPeriod()
    {
        return Random.Next(MinTime, MaxTime);
    }
}
