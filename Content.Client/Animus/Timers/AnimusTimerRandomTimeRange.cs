using Robust.Client.Animus.Timers;

namespace Content.Client.Animus.Timers;

public sealed partial class AnimusTimerRandomTimeRange : AnimusTimerBase
{
    [DataField]
    public TimeSpan MinTime;

    [DataField]
    public TimeSpan MaxTime;

    public override TimeSpan GetNextPeriod()
    {
        return Random.Next(MinTime, MaxTime);
    }
}
