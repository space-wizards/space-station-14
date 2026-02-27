using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Spawners.Components;
using Content.Shared.Whitelist;
using Robust.Server.Physics;
using Robust.Shared.Map;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// Handles storing grids from <see cref="RuleLoadedGridsEvent"/> and antags spawning on their spawners.
/// </summary>
public sealed class RuleGridsSystem : GameRuleSystem<RuleGridsComponent>
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GridSplitEvent>(OnGridSplit);

        SubscribeLocalEvent<RuleGridsComponent, RuleLoadedGridsEvent>(OnLoadedGrids);
        SubscribeLocalEvent<RuleGridsComponent, AntagSelectLocationEvent>(OnSelectLocation);
    }

    private void OnGridSplit(ref GridSplitEvent args)
    {
        var rule = QueryActiveRules();
        while (rule.MoveNext(out _, out var comp, out _))
        {
            if (!comp.MapGrids.Contains(args.Grid))
                continue;

            comp.MapGrids.AddRange(args.NewGrids);
            break; // only 1 rule can own a grid, not multiple
        }
    }

    private void OnLoadedGrids(Entity<RuleGridsComponent> ent, ref RuleLoadedGridsEvent args)
    {
        var (uid, comp) = ent;
        if (comp.Map != null && args.Map != comp.Map)
        {
            Log.Warning($"{ToPrettyString(uid):rule} loaded grids on multiple maps {comp.Map} and {args.Map}, the second will be ignored.");
            return;
        }

        comp.Map = args.Map;
        comp.MapGrids.AddRange(args.Grids);
    }

    private void OnSelectLocation(Entity<RuleGridsComponent> ent, ref AntagSelectLocationEvent args)
    {
        var query = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapID != ent.Comp.Map)
                continue;

            if (xform.GridUid is not {} grid || !ent.Comp.MapGrids.Contains(grid))
                continue;

            if (_whitelist.IsWhitelistFail(ent.Comp.SpawnerWhitelist, uid))
                continue;

            if (TryComp<GridSpawnPointWhitelistComponent>(uid, out var gridSpawnPointWhitelistComponent))
            {
                if (!_whitelist.CheckBoth(args.Entity, gridSpawnPointWhitelistComponent.Blacklist, gridSpawnPointWhitelistComponent.Whitelist))
                    continue;
            }

            args.Coordinates.Add(_transform.GetMapCoordinates(xform));
        }
    }
}

/// <summary>
/// Raised by another gamerule system to store loaded grids, and have other systems work with it.
/// A single rule can only load grids for a single map, attempts to load more are ignored.
/// </summary>
[ByRefEvent]
public record struct RuleLoadedGridsEvent(MapId Map, IReadOnlyList<EntityUid> Grids);
