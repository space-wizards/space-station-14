using Robust.Client.Animus.Timers;
using Robust.Shared.Random;

namespace Content.Client.Animus.Timers;

public sealed partial class AnimusTimerRandomTimeRange : AnimusTimerBase
{
    [DataField]
    public TimeSpan MinTime;

    [DataField]
    public TimeSpan MaxTime;

    public override TimeSpan GetNextPeriod(IRobustRandom random)
    {
        return random.Next(MinTime, MaxTime);
    }
}
