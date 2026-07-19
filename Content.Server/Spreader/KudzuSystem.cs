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
    private static readonly EntityTimerId GrowthTimer = new("growth");

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _robustRandom = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IEntityTimerManager _timers = default!;

    [Dependency] private EntityQuery<AppearanceComponent> _appearanceQuery = default!;
    [Dependency] private EntityQuery<KudzuComponent> _kudzuQuery = default!;
    [Dependency] private EntityQuery<DamageableComponent> _damageableQuery = default!;

    private static readonly ProtoId<EdgeSpreaderPrototype> KudzuGroup = "Kudzu";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<KudzuComponent, ComponentStartup>(SetupKudzu);
        SubscribeLocalEvent<KudzuComponent, SpreadNeighborsEvent>(OnKudzuSpread);
        SubscribeLocalEvent<KudzuComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<GrowingKudzuComponent, ComponentStartup>(OnGrowingStartup);
        SubscribeLocalEvent<GrowingKudzuComponent, EntityTimerEvent>(OnGrowthTimer);
    }

    private void OnGrowingStartup(Entity<GrowingKudzuComponent> ent, ref ComponentStartup args)
    {
        _timers.SetTimerAt(ent, GrowthTimer, ent.Comp.NextTick);
    }

    private void OnDamageChanged(EntityUid uid, KudzuComponent component, DamageChangedEvent args)
    {
        // Every time we take any damage, we reduce growth depending on all damage over the growth impact
        //   So the kudzu gets slower growing the more it is hurt.
        var growthDamage = (int) (_damageable.GetTotalDamage((uid, args.Damageable)) / component.GrowthHealth);
        if (growthDamage > 0)
        {
            if (!EnsureComp<GrowingKudzuComponent>(uid, out _))
                component.GrowthLevel = 3;

            component.GrowthLevel = Math.Max(1, component.GrowthLevel - growthDamage);
            if (TryComp<AppearanceComponent>(uid, out var appearance))
            {
                _appearance.SetData(uid, KudzuVisuals.GrowthLevel, component.GrowthLevel, appearance);
            }
        }
    }

    private void OnKudzuSpread(EntityUid uid, KudzuComponent component, ref SpreadNeighborsEvent args)
    {
        if (component.GrowthLevel < 3)
            return;

        if (args.NeighborFreeTiles.Count == 0)
        {
            RemCompDeferred<ActiveEdgeSpreaderComponent>(uid);
            return;
        }

        if (!_robustRandom.Prob(component.SpreadChance))
            return;

        var prototype = MetaData(uid).EntityPrototype?.ID;

        if (prototype == null)
        {
            RemCompDeferred<ActiveEdgeSpreaderComponent>(uid);
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

    private void SetupKudzu(EntityUid uid, KudzuComponent component, ComponentStartup args)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
        {
            return;
        }

        _appearance.SetData(uid, KudzuVisuals.Variant, _robustRandom.Next(1, component.SpriteVariants), appearance);
        _appearance.SetData(uid, KudzuVisuals.GrowthLevel, 1, appearance);
    }

    /// <inheritdoc/>
    private void OnGrowthTimer(Entity<GrowingKudzuComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != GrowthTimer)
            return;

        var grow = ent.Comp;
        grow.NextTick = args.FiredAt + TimeSpan.FromSeconds(0.5);
        _timers.SetTimerAt(ent, GrowthTimer, grow.NextTick);

        if (!_kudzuQuery.TryGetComponent(ent, out var kudzu))
        {
            RemCompDeferred(ent, grow);
            return;
        }

        if (!_robustRandom.Prob(kudzu.GrowthTickChance))
            return;

            if (_damageableQuery.TryGetComponent(ent, out var damage))
            {
                var totalDamage = _damageable.GetTotalDamage((ent.Owner, damage));
                if (totalDamage > 1.0)
                {
                    if (kudzu.DamageRecovery != null)
                    {
                        // This kudzu features healing, so Gradually heal
                        _damageable.TryChangeDamage(ent.Owner, kudzu.DamageRecovery, true);
                    }
                    if (totalDamage >= kudzu.GrowthBlock)
                    {
                        // Don't grow when quite damaged
                        if (_robustRandom.Prob(0.95f))
                        {
                            return;
                        }
                    }
                }
            }

            kudzu.GrowthLevel += 1;

            if (kudzu.GrowthLevel >= 3)
            {
                // why cache when you can simply cease to be? Also saves a bit of memory/time.
                RemCompDeferred(ent, grow);
            }

            if (_appearanceQuery.TryGetComponent(ent, out var appearance))
            {
                _appearance.SetData(ent, KudzuVisuals.GrowthLevel, kudzu.GrowthLevel, appearance);
            }
    }
}
