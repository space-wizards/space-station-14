// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Timing;

namespace Content.Shared.DeadSpace.Necromorphs.Sanity;

public abstract class SharedSanitySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SanityComponent, EntityUnpausedEvent>(OnSanityUnpause);
    }

    private void OnSanityUnpause(EntityUid uid, SanityComponent component, ref EntityUnpausedEvent args)
    {
        component.NextTickUtilRegen += args.PausedTime;
    }

    public bool TryAddSanityLvl(EntityUid uid, float lvl, SanityComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        component.SanityLevel += lvl;

        if (component.SanityLevel > component.MaxSanityLevel)
            component.SanityLevel = component.MaxSanityLevel;

        if (component.SanityLevel < component.MinSanityLevel)
            component.SanityLevel = component.MaxSanityLevel;

        var crazyMobEvent = new CheckCrazyMobEvent();
        RaiseLocalEvent(uid, ref crazyMobEvent);

        return true;
    }

    public void UpdateSanity(EntityUid uid, bool needRegen = false, SanityComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!_gameTiming.IsFirstTimePredicted)
            return;

        if (needRegen)
            TryAddSanityLvl(uid, component.RegenSanity);

        var sanityEvent = new SanityEvent();
        RaiseLocalEvent(uid, ref sanityEvent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var sanityQuery = EntityQueryEnumerator<SanityComponent>();
        while (sanityQuery.MoveNext(out var ent, out var sanity))
        {
            if (_gameTiming.CurTime > sanity.NextTickUtilRegen)
            {
                UpdateSanity(ent, true, sanity);
                sanity.NextTickUtilRegen = _gameTiming.CurTime + TimeSpan.FromSeconds(sanity.UpdateDuration);
            }
        }
    }
}
