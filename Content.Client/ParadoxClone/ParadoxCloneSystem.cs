using Content.Shared.ParadoxClone;
using Robust.Shared.Timing;

namespace Content.Client.ParadoxClone;

// literally just here for the alerts and for prediction to work
public sealed partial class ParadoxCloneSystem : SharedParadoxCloneSystem
{
    [Dependency] private IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        // otherwise the timers go down very very very very fast
        if (!_timing.IsFirstTimePredicted)
            return;

        base.Update(frameTime);
    }
}
