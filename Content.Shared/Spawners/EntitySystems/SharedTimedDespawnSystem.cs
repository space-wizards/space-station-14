using Content.Shared.Spawners.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Spawners.EntitySystems;

public abstract class SharedTimedDespawnSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesOutsidePrediction = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // AAAAAAAAAAAAAAAAAAAAAAAAAAA
        // Client both needs to predict this, but also can't properly handle prediction resetting.
        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<TimedDespawnComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            comp.Lifetime -= frameTime;

            if (!CanDelete(uid))
                continue;

            if (comp.Lifetime <= 0)
            {
                var ev = new TimedDespawnEvent();
                RaiseLocalEvent(uid, ref ev);
                QueueDel(uid);
            }
        }
    }

    protected abstract bool CanDelete(EntityUid uid);
}
