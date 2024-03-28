using Robust.Shared.Map.Components;
using Robust.Shared.Timing;

namespace Content.Shared.CrystallPunk.DayCycle;
public sealed partial class DayCycleSystem : EntitySystem
{

    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DayCycleComponent, MapInitEvent>(OnMapInitDayCycle);
        SubscribeLocalEvent<DayCycleComponent, DayCycleDayStartedEvent>(OnDayStarted);
        SubscribeLocalEvent<DayCycleComponent, DayCycleNightStartedEvent>(OnNightStarted);
    }

    private void OnDayStarted(Entity<DayCycleComponent> dayCycle, ref DayCycleDayStartedEvent args)
    {
    }

    private void OnNightStarted(Entity<DayCycleComponent> dayCycle, ref DayCycleNightStartedEvent args)
    {
    }

    private void OnMapInitDayCycle(Entity<DayCycleComponent> dayCycle, ref MapInitEvent args)
    {
        if (dayCycle.Comp.TimeEntries == null || dayCycle.Comp.TimeEntries.Count == 0) return;

        var currentEntry = dayCycle.Comp.TimeEntries[0];

        dayCycle.Comp.EntryStartTime = _timing.CurTime;
        dayCycle.Comp.EntryEndTime = _timing.CurTime + currentEntry.Duration;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var dayCycleQuery = EntityQueryEnumerator<DayCycleComponent, MapLightComponent>();
        while (dayCycleQuery.MoveNext(out var uid, out var dayCycle, out var mapLight))
        {
            if (dayCycle.TimeEntries.Count <= 1) continue;

            var curEntry = dayCycle.CurrentTimeEntry;
            var nextEntry = (curEntry + 1 >= dayCycle.TimeEntries.Count) ? 0 : (curEntry + 1);

            var start = dayCycle.EntryStartTime;
            var end = dayCycle.EntryEndTime;

            var lerpValue = GetLerpValue((float) start.TotalSeconds, (float) end.TotalSeconds, (float) _timing.CurTime.TotalSeconds);

            var startColor = dayCycle.TimeEntries[curEntry].StartColor;
            var endColor = dayCycle.TimeEntries[nextEntry].StartColor;

            mapLight.AmbientLightColor = Color.InterpolateBetween(startColor, endColor, lerpValue);
            Dirty(uid, mapLight);


            if (_timing.CurTime > dayCycle.EntryEndTime)
            {
                dayCycle.CurrentTimeEntry = nextEntry;
                dayCycle.EntryStartTime = dayCycle.EntryEndTime;
                dayCycle.EntryEndTime = dayCycle.EntryEndTime + dayCycle.TimeEntries[nextEntry].Duration;

                if (dayCycle.IsNight && !dayCycle.TimeEntries[curEntry].IsNight) // Day started
                {
                    dayCycle.IsNight = false;
                    var ev = new DayCycleDayStartedEvent(uid);
                    RaiseLocalEvent(uid, ref ev, true);
                }
                if (!dayCycle.IsNight && dayCycle.TimeEntries[curEntry].IsNight) // Night started
                {
                    dayCycle.IsNight = true;
                    var ev = new DayCycleNightStartedEvent(uid);
                    RaiseLocalEvent(uid, ref ev, true);
                }
            }
        }
    }

    public static float GetLerpValue(float start, float end, float current)
    {
        if (start == end)
            return 0f;
        else
        {
            float distanceFromStart = current - start;
            float totalDistance = end - start;

            return MathHelper.Clamp01(distanceFromStart / totalDistance);
        }
    }
}
