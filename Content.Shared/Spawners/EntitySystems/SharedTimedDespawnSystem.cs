using Content.Shared.Spawners.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Spawners.EntitySystems;

public abstract class SharedTimedDespawnSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TimedDespawnComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, TimedDespawnComponent component, ComponentInit args)
    {
        if (!component.RandomLifetime)
            return;

        component.Lifetime = _random.NextFloat(component.MinRandomLifetime, component.MaxRandomLifetime);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted) return;

        foreach (var comp in EntityQuery<TimedDespawnComponent>())
        {
            if (!CanDelete(comp.Owner)) continue;

            comp.Lifetime -= frameTime;

            if (comp.Lifetime <= 0)
                EntityManager.QueueDeleteEntity(comp.Owner);
        }
    }

    protected abstract bool CanDelete(EntityUid uid);
}
