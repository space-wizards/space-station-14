using Content.Shared.Spreader;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Spreader;

public sealed class KudzuSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    private const string KudzuGroup = "kudzu";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<KudzuComponent, ComponentStartup>(SetupKudzu);
        SubscribeLocalEvent<KudzuComponent, SpreadNeighborsEvent>(OnKudzuSpread);
        SubscribeLocalEvent<GrowingKudzuComponent, EntityUnpausedEvent>(OnKudzuUnpaused);
        SubscribeLocalEvent<SpreadGroupUpdateRate>(OnKudzuUpdateRate);
    }

    private void OnKudzuSpread(EntityUid uid, KudzuComponent component, ref SpreadNeighborsEvent args)
    {
        if (TryComp<GrowingKudzuComponent>(uid, out var growing) && growing.GrowthLevel < 3)
        {
            return;
        }

        if (args.NeighborFreeTiles.Count == 0)
        {
            RemCompDeferred<EdgeSpreaderComponent>(uid);
            return;
        }

        var prototype = MetaData(uid).EntityPrototype?.ID;

        if (prototype == null)
        {
            RemCompDeferred<EdgeSpreaderComponent>(uid);
            return;
        }

        if (!_robustRandom.Prob(component.SpreadChance))
            return;

        foreach (var neighbor in args.NeighborFreeTiles)
        {
            var neighborUid = Spawn(prototype, neighbor.Grid.GridTileToLocal(neighbor.Tile));
            EnsureComp<EdgeSpreaderComponent>(neighborUid);
            args.Updates--;

            if (args.Updates <= 0)
                return;
        }
    }

    private void OnKudzuUpdateRate(ref SpreadGroupUpdateRate args)
    {
        if (args.Name != KudzuGroup)
            return;

        args.UpdatesPerSecond = 1;
    }

    private void OnKudzuUnpaused(EntityUid uid, GrowingKudzuComponent component, ref EntityUnpausedEvent args)
    {
        component.NextTick += args.PausedTime;
    }

    private void SetupKudzu(EntityUid uid, KudzuComponent component, ComponentStartup args)
    {
        if (!EntityManager.TryGetComponent<AppearanceComponent>(uid, out var appearance))
        {
            return;
        }

        _appearance.SetData(uid, KudzuVisuals.Variant, _robustRandom.Next(1, 3), appearance);
        _appearance.SetData(uid, KudzuVisuals.GrowthLevel, 1, appearance);
    }

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<GrowingKudzuComponent, AppearanceComponent>();
        var curTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var kudzu, out var appearance))
        {
            if (kudzu.NextTick > curTime)
            {
                continue;
            }

            kudzu.NextTick = curTime + TimeSpan.FromSeconds(0.5);

            if (!_robustRandom.Prob(kudzu.GrowthTickChance))
            {
                continue;
            }

            kudzu.GrowthLevel += 1;

            if (kudzu.GrowthLevel >= 3)
            {
                // why cache when you can simply cease to be? Also saves a bit of memory/time.
                RemCompDeferred<GrowingKudzuComponent>(uid);
            }

            _appearance.SetData(uid, KudzuVisuals.GrowthLevel, kudzu.GrowthLevel, appearance);
        }
    }
}
