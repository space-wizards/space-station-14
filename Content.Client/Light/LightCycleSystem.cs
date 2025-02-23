using Content.Client.GameTicking.Managers;
using Content.Shared;
using Content.Shared.Light.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Client.Light;

/// <inheritdoc/>
public sealed class LightCycleSystem : SharedLightCycleSystem
{
    [Dependency] private readonly ClientGameTicker _ticker = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var mapQuery = AllEntityQuery<LightCycleComponent, MapLightComponent>();
        while (mapQuery.MoveNext(out var uid,  out var cycle, out var map))
        {
            if (!cycle.Running)
                continue;

            var time = (float) _timing.CurTime
                .Add(cycle.Offset)
                .Subtract(_ticker.RoundStartTimeSpan)
                .TotalSeconds;

            var color = GetColor((uid, cycle), cycle.OriginalColor, time);
            map.AmbientLightColor = color;
        }
    }
}
