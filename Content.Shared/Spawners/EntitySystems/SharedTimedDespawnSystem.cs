using Content.Shared.Spawners.Components;
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

        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<TimedDespawnComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (!CanDelete(uid))
                continue;

            comp.Lifetime -= frameTime;

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

/// <summary>
/// Raised directed on an entity when its timed despawn is over.
/// </summary>
[ByRefEvent]
public readonly record struct TimedDespawnEvent;
