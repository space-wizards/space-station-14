using System.Diagnostics.Contracts;
using System.Numerics;
using Content.Client.GameTicking.Managers;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

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
        var ratio = (float) (time / entity.Comp.Duration.TotalSeconds);

        for (var i = entity.Comp.Directions.Count - 1; i >= 0; i--)
        {
            var dir = entity.Comp.Directions[i];

            if (ratio > dir.Ratio)
            {
                var next = entity.Comp.Directions[(i + 1) % entity.Comp.Directions.Count];
                float nextRatio;

                // Last entry
                if (i == entity.Comp.Directions.Count - 1)
                {
                    nextRatio = next.Ratio + 1f;
                }
                else
                {
                    nextRatio = next.Ratio;
                }

                var range = nextRatio - dir.Ratio;
                var diff = (ratio - dir.Ratio) / range;
                DebugTools.Assert(diff is >= 0f and <= 1f);

                // We lerp angle + length separately as we don't want a straight-line lerp and want the rotation to be consistent.
                var currentAngle = dir.Direction.ToAngle();
                var nextAngle = next.Direction.ToAngle();

                var angle = Angle.Lerp(currentAngle, nextAngle, diff);
                // This is to avoid getting weird issues where the angle gets pretty close but length still noticeable catches up.
                var lengthDiff = MathF.Pow(diff, 1f / 2f);
                var length = float.Lerp(dir.Direction.Length(), next.Direction.Length(), lengthDiff);

                var vector = angle.ToVec() * length;
                var alpha = float.Lerp(dir.Alpha, next.Alpha, diff);
                return (vector, alpha);
            }
        }

        throw new InvalidOperationException();
    }
}
