using System.Diagnostics.Contracts;
using System.Numerics;
using Content.Client.GameTicking.Managers;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Robust.Shared.Timing;

namespace Content.Client.Light.EntitySystems;

public sealed class SunShadowSystem : SharedSunShadowSystem
{
    [Dependency] private readonly ClientGameTicker _ticker = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var mapQuery = AllEntityQuery<SunShadowCycleComponent, SunShadowComponent>();
        while (mapQuery.MoveNext(out var uid,  out var cycle, out var shadow))
        {
            if (!cycle.Running || cycle.Directions.Count == 0)
                continue;

            var time = (float)(_timing.CurTime
                .Add(cycle.Offset)
                .Subtract(_ticker.RoundStartTimeSpan)
                .TotalSeconds % cycle.Duration.TotalSeconds);

            var (direction, alpha) = GetShadow((uid, cycle), time);
            shadow.Direction = direction;
            shadow.Alpha = alpha;
        }
    }

    [Pure]
    public (Vector2 Direction, float Alpha) GetShadow(Entity<SunShadowCycleComponent> entity, float time)
    {
        for (var i = 0; i < entity.Comp.Directions.Count; i++)
        {
            var dir = entity.Comp.Directions[i];

            if (time < dir.Time.TotalSeconds)
            {
                var last = entity.Comp.Directions[(i + entity.Comp.Directions.Count - 1) % entity.Comp.Directions.Count];
                var diff = (float) ((time - last.Time.TotalSeconds) / (dir.Time.TotalSeconds - last.Time.TotalSeconds));

                // We lerp angle + length separately as we don't want a straight-line lerp and want the rotation to be consistent.
                var currentAngle = dir.Direction.ToAngle();
                var lastAngle = last.Direction.ToAngle();

                var angle = Angle.Lerp(lastAngle, currentAngle, diff);
                var length = float.Lerp(last.Direction.Length(), dir.Direction.Length(), diff);

                var vector = angle.ToVec() * length;
                var alpha = float.Lerp(last.Alpha, dir.Alpha, diff);
                return (vector, alpha);
            }
        }

        throw new InvalidOperationException();
    }
}
