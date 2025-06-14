using Content.Client.GameTicking.Managers;
using Content.Shared;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Client.Light;

/// <inheritdoc/>
public sealed class LightCycleSystem : SharedLightCycleSystem
{
    [Dependency] private readonly ClientGameTicker _ticker = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var mapQuery = AllEntityQuery<LightCycleComponent, MapLightComponent>();
        while (mapQuery.MoveNext(out var uid,  out var cycle, out var map))
        {
            if (!cycle.Running)
                continue;

            // We still iterate paused entities as we still want to override the lighting color and not have
            // it apply the server state
            var pausedTime = _metadata.GetPauseTime(uid);

            var time = (float) _timing.CurTime
                .Add(cycle.Offset)
                .Subtract(_ticker.RoundStartTimeSpan)
                .Subtract(pausedTime)
                .TotalSeconds;

            var color = GetColor((uid, cycle), cycle.OriginalColor, time);
            map.AmbientLightColor = color;
        }
    }
}
