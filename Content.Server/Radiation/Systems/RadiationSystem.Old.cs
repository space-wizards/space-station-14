using Content.Shared.Radiation.Events;
using Robust.Shared.Timing;

namespace Content.Server.Radiation.Systems;

public partial class RadiationSystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private void UpdateOld()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        foreach (var source in EntityQuery<RadiationSourceComponent>())
        {
            var ents = _lookup.GetEntitiesInRange(source.Owner, source.Range);
            foreach (var uid in ents)
            {
                RaiseLocalEvent(uid, new OnIrradiatedEvent(1f, 1f));
            }
        }

        Logger.Info($"Range radiation {stopwatch.Elapsed.TotalMilliseconds}ms");
    }
}
