using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Spreader;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Spreader;

public sealed partial class KudzuSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _robustRandom = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private DamageableSystem _damageable = default!;

    [Dependency] private EntityQuery<AppearanceComponent> _appearanceQuery = default!;
    [Dependency] private EntityQuery<KudzuComponent> _kudzuQuery = default!;
    [Dependency] private EntityQuery<DamageableComponent> _damageableQuery = default!;

    private static readonly ProtoId<EdgeSpreaderPrototype> KudzuGroup = "Kudzu";

    [SubscribeLocalEvent]
    private void OnDamageChanged(Entity<KudzuComponent> ent, ref DamageChangedEvent args)
    {
        // Every time we take any damage, we reduce growth depending on all damage over the growth impact
        //   So the kudzu gets slower growing the more it is hurt.
        var growthDamage = (int)(_damageable.GetTotalDamage((ent, args.Damageable)) / ent.Comp.GrowthHealth);
        if (growthDamage > 0)
        {
            if (!EnsureComp<GrowingKudzuComponent>(ent, out _))
                ent.Comp.GrowthLevel = 3;

            ent.Comp.GrowthLevel = Math.Max(1, ent.Comp.GrowthLevel - growthDamage);
            _appearance.SetData(ent, KudzuVisuals.GrowthLevel, ent.Comp.GrowthLevel);
        }
    }

    [SubscribeLocalEvent]
    private void OnKudzuSpread(Entity<KudzuComponent> ent, ref SpreadNeighborsEvent args)
    {
        if (ent.Comp.GrowthLevel < 3)
            return;

        if (args.NeighborFreeTiles.Count == 0)
        {
            RemCompDeferred<ActiveEdgeSpreaderComponent>(ent);
            return;
        }

        if (!_robustRandom.Prob(ent.Comp.SpreadChance))
            return;

        var prototype = MetaData(ent).EntityPrototype?.ID;

        if (prototype == null)
        {
            RemCompDeferred<ActiveEdgeSpreaderComponent>(ent);
            return;
        }

        foreach (var neighbor in args.NeighborFreeTiles)
        {
            var neighborUid = Spawn(prototype, _map.GridTileToLocal(neighbor.Tile.GridUid, neighbor.Grid, neighbor.Tile.GridIndices));
            DebugTools.Assert(HasComp<EdgeSpreaderComponent>(neighborUid));
            DebugTools.Assert(HasComp<ActiveEdgeSpreaderComponent>(neighborUid));
            DebugTools.Assert(Comp<EdgeSpreaderComponent>(neighborUid).Id == KudzuGroup);
            args.Updates--;
            if (args.Updates <= 0)
                return;
        }
    }

    [SubscribeLocalEvent]
    private void SetupKudzu(Entity<KudzuComponent> ent, ref ComponentStartup args)
    {
        if (!_appearanceQuery.TryComp(ent, out var appearance))
            return;

        _appearance.SetData(ent, KudzuVisuals.Variant, _robustRandom.Next(1, ent.Comp.SpriteVariants), appearance);
        _appearance.SetData(ent, KudzuVisuals.GrowthLevel, 1, appearance);
    }

    /// <inheritdoc/>
    public override void Update(float frameTime)
    {
        var kudzuEnumerator = EntityQueryEnumerator<GrowingKudzuComponent>();
        var curTime = _timing.CurTime;

        while (kudzuEnumerator.MoveNext(out var uid, out var grow))
        {
            if (grow.NextTick > curTime)
                continue;

            grow.NextTick = curTime + TimeSpan.FromSeconds(0.5);

            if (!_kudzuQuery.TryGetComponent(uid, out var kudzu))
            {
                RemCompDeferred(uid, grow);
                continue;
            }

            if (!_robustRandom.Prob(kudzu.GrowthTickChance))
            {
                continue;
            }

            if (_damageableQuery.TryGetComponent(uid, out var damage))
            {
                var totalDamage = _damageable.GetTotalDamage((uid, damage));
                if (totalDamage > 1.0)
                {
                    if (kudzu.DamageRecovery != null)
                    {
                        // This kudzu features healing, so Gradually heal
                        _damageable.TryChangeDamage(uid, kudzu.DamageRecovery, true);
                    }
                    if (totalDamage >= kudzu.GrowthBlock)
                    {
                        // Don't grow when quite damaged
                        if (_robustRandom.Prob(0.95f))
                        {
                            continue;
                        }
                    }
                }
            }

            kudzu.GrowthLevel += 1;

            if (kudzu.GrowthLevel >= 3)
            {
                // why cache when you can simply cease to be? Also saves a bit of memory/time.
                RemCompDeferred(uid, grow);
            }

            _appearance.SetData(uid, KudzuVisuals.GrowthLevel, kudzu.GrowthLevel);
        }
    }
}
