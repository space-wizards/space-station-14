using Content.Shared.Radiation.Components;
using Content.Shared.Spawners.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Radiation.Systems;

public sealed class SharedRadiationPulseSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadiationPulseComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(EntityUid uid, RadiationPulseComponent component, ComponentStartup args)
    {
        component.StartTime = _timing.CurTime;

        // try to get despawn time or keep default
        if (TryComp<TimedDespawnComponent>(uid, out var despawn))
        {
            component.VisualDuration = despawn.Lifetime;
        }
        // try to get radiation range or keep default
        if (TryComp<RadiationSourceComponent>(uid, out var radSource))
        {
            component.VisualRange = radSource.Range;
        }
    }
}
