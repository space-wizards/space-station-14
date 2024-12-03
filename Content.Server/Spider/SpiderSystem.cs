using System.Linq;
using Content.Server.Popups;
using Content.Shared.Spider;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Spider;

public sealed class SpiderSystem : SharedSpiderSystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly ITileDefinitionManager _tile = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpiderComponent, SpiderWebActionEvent>(OnSpawnNet);
    }

    private void OnSpawnNet(EntityUid uid, SpiderComponent component, SpiderWebActionEvent args)
    {
        if (args.Handled)
            return;

        var transform = Transform(uid);
        var grid = transform.GridUid;

        if (grid == null || !TryComp<MapGridComponent>(grid, out var gridComp))
        {
            _popup.PopupEntity(Loc.GetString("spider-web-action-nogrid"), args.Performer, args.Performer);
            return;
        }

        var coords = transform.Coordinates;
        List<EntityCoordinates> spawnPos = new();

        var vects = new List<Vector2i>();
        vects.AddRange(args.OffsetVectors);

        foreach (var vect in vects)
            spawnPos.Add(coords.Offset(vect));

        bool success = false;
        foreach (var pos in spawnPos)
            if (IsValidTile(pos, grid.Value, gridComp))
            {
                Spawn(component.WebPrototype, pos);
                success = true;
            }

        if (success)
        {
            _popup.PopupEntity(Loc.GetString("spider-web-action-success"), args.Performer, args.Performer);
            args.Handled = true;
        }
        else
            _popup.PopupEntity(Loc.GetString("spider-web-action-fail"), args.Performer, args.Performer);
    }

    private bool IsValidTile(EntityCoordinates coords, EntityUid grid, MapGridComponent gridComp)
    {
        // TODO change this hard coded comp
        // Don't place webs on top of webs
        foreach (var entity in _lookup.GetEntitiesIntersecting(coords, LookupFlags.Uncontained))
            if (HasComp<SpiderWebObjectComponent>(entity))
                return false;

        // Don't place webs in space
        if (!_map.TryGetTileRef(grid, gridComp, coords, out var tileRef) ||
            tileRef.IsSpace(_tile))
                return false;

        return true;
    }
}
