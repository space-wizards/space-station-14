using Content.Shared.Damage;
using Content.Shared.Spreader;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Spreader;

public sealed class KudzuSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    private const string KudzuGroup = "kudzu";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<KudzuComponent, ComponentStartup>(SetupKudzu);
        SubscribeLocalEvent<KudzuComponent, SpreadNeighborsEvent>(OnKudzuSpread);
        SubscribeLocalEvent<GrowingKudzuComponent, EntityUnpausedEvent>(OnKudzuUnpaused);
        SubscribeLocalEvent<SpreadGroupUpdateRate>(OnKudzuUpdateRate);
        SubscribeLocalEvent<KudzuComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnDamageChanged(EntityUid uid, KudzuComponent component, DamageChangedEvent args)
    {
        // Every time we take any damage, we reduce growth depending on all damage over the growth impact
        //   So the kudzu gets slower growing the more it is hurt.
        int growthDamage = (int) (args.Damageable.TotalDamage / component.GrowthHealth);
        if (growthDamage > 0)
        {
            GrowingKudzuComponent? growing;
            if (!TryComp(uid, out growing))
            {
                growing = AddComp<GrowingKudzuComponent>(uid);
                growing.GrowthLevel = 3;
            }
            growing.GrowthLevel = Math.Max(1, growing.GrowthLevel - growthDamage);
            if (EntityManager.TryGetComponent<AppearanceComponent>(uid, out var appearance))
            {
                _appearance.SetData(uid, KudzuVisuals.GrowthLevel, growing.GrowthLevel, appearance);
            }
        }
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
        var appearanceQuery = GetEntityQuery<AppearanceComponent>();
        var query = EntityQueryEnumerator<GrowingKudzuComponent, KudzuComponent>();
        var curTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var grow, out var kudzu))
        {
            if (grow.NextTick > curTime)
            {
                continue;
            }

            grow.NextTick = curTime + TimeSpan.FromSeconds(0.5);

            if (!_robustRandom.Prob(kudzu.GrowthTickChance))
            {
                continue;
            }

            if (TryComp<DamageableComponent>(uid, out var damage))
            {
                if (damage.TotalDamage > 1.0)
                {
                    if (kudzu.DamageRecovery != null)
                    {
                        // This kudzu features healing, so Gradually heal
                        _damageable.TryChangeDamage(uid, kudzu.DamageRecovery, true);
                    }
                    if (damage.TotalDamage >= kudzu.GrowthBlock)
                    {
                        // Don't grow when quite damaged
                        if (_robustRandom.Prob(0.95f))
                        {
                            continue;
                        }
                    }
                }
            }

            grow.GrowthLevel += 1;

            if (grow.GrowthLevel >= 3)
            {
                // why cache when you can simply cease to be? Also saves a bit of memory/time.
                RemCompDeferred<GrowingKudzuComponent>(uid);
            }

            if (appearanceQuery.TryGetComponent(uid, out var appearance))
            {
                _appearance.SetData(uid, KudzuVisuals.GrowthLevel, grow.GrowthLevel, appearance);
            }
        }
    }
}
