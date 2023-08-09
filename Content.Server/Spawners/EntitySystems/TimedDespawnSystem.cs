using Content.Server.Spawners.Components;
using Content.Shared.Spawners.EntitySystems;

namespace Content.Server.Spawners.EntitySystems;

public sealed class TimedDespawnSystem : SharedTimedDespawnSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TimedSpawnerComponent, ComponentShutdown>(OnTimedSpawnerShutdown);
    }

    private void OnTimedSpawnerShutdown(EntityUid uid, TimedSpawnerComponent component, ComponentShutdown args)
    {
        component.TokenSource?.Cancel();
    }

    protected override bool CanDelete(EntityUid uid)
    {
        return true;
    }
}
