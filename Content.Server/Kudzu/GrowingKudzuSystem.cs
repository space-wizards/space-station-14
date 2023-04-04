using Content.Shared.Kudzu;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Kudzu;

public sealed class GrowingKudzuSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GrowingKudzuComponent, ComponentStartup>(SetupKudzu);
        SubscribeLocalEvent<GrowingKudzuComponent, EntityUnpausedEvent>(OnKudzuUnpaused);
    }

    private void OnKudzuUnpaused(EntityUid uid, GrowingKudzuComponent component, ref EntityUnpausedEvent args)
    {
        component.NextTick += args.PausedTime;
    }

    private void SetupKudzu(EntityUid uid, GrowingKudzuComponent component, ComponentStartup args)
    {
        if (!EntityManager.TryGetComponent<AppearanceComponent>(uid, out var appearance))
        {
            return;
        }

        _appearance.SetData(uid, KudzuVisuals.Variant, _robustRandom.Next(1, 3), appearance);
        _appearance.SetData(uid, KudzuVisuals.GrowthLevel, 1, appearance);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<GrowingKudzuComponent, AppearanceComponent>();
        var curTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var kudzu, out var appearance))
        {
            if (kudzu.GrowthLevel >= 3 ||
                kudzu.NextTick < curTime ||
                !_robustRandom.Prob(kudzu.GrowthTickSkipChange))
            {
                continue;
            }

            // Tickrate dependent but means we don't need to bother book-keeping nexttick.
            kudzu.NextTick = curTime + TimeSpan.FromSeconds(0.5);
            kudzu.GrowthLevel += 1;

            /*
            if (kudzu.GrowthLevel == 3 &&
                HasComp<SpreaderComponent>(uid))
            {
                // why cache when you can simply cease to be? Also saves a bit of memory/time.
                RemCompDeferred<GrowingKudzuComponent>(uid);
            }

            _appearance.SetData(uid, KudzuVisuals.GrowthLevel, kudzu.GrowthLevel, appearance);
            */
        }
    }
}
