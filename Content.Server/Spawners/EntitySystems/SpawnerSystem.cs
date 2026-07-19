using Content.Server.Spawners.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Spawners.EntitySystems;

public sealed partial class SpawnerSystem : EntitySystem
{
    private static readonly EntityTimerId SpawnTimer = new("spawn");

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private EntityTimerSystem _timers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TimedSpawnerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TimedSpawnerComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnTimer(Entity<TimedSpawnerComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != SpawnTimer)
            return;

        ent.Comp.NextFire = args.NextDeadline ?? args.ScheduledTime + ent.Comp.IntervalSeconds;
        OnTimerFired(ent, ent.Comp);
    }

    private void OnMapInit(Entity<TimedSpawnerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextFire = _timing.CurTime + ent.Comp.IntervalSeconds;
        _timers.SetTimerAt(ent, SpawnTimer, ent.Comp.NextFire, ent.Comp.IntervalSeconds);
    }

    private void OnTimerFired(EntityUid uid, TimedSpawnerComponent component)
    {
        if (!_random.Prob(component.Chance))
            return;

        var number = _random.Next(component.MinimumEntitiesSpawned, component.MaximumEntitiesSpawned);
        var coordinates = Transform(uid).Coordinates;

        for (var i = 0; i < number; i++)
        {
            var entity = _random.Pick(component.Prototypes);
            SpawnAtPosition(entity, coordinates);
        }
    }
}
