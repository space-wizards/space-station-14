using System.Linq;
using Content.Server.Popups;
using Content.Shared.Maps;
using Content.Shared.Spider;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Spider;

/// <summary>
/// Spawns entities (probably webs) around the component owner. Action handled by <see cref="SharedSpiderSystem"/>.
/// </summary>
public sealed class SpiderSystem : SharedSpiderSystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly ITileDefinitionManager _tile = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

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
            _popup.PopupEntity(Loc.GetString(component._offGrid), args.Performer, args.Performer);
            return;
        }

        var coords = transform.Coordinates;
        List<EntityCoordinates> spawnPos = new();

        var vects = new List<Vector2i>();
        vects.AddRange(args.OffsetVectors);

        foreach (var vect in vects)
            spawnPos.Add(coords.Offset(vect));

        // Spawn webs here
        bool success = false;
        foreach (var pos in spawnPos)
            if (IsValidTile(pos, component.DestinationWhitelist, component.DestinationBlacklist, grid.Value, gridComp))
            {
                Spawn(component.WebPrototype, pos);
                success = true;
            }

        if (success)
        {
            _popup.PopupEntity(Loc.GetString(component._success), args.Performer, args.Performer);
            args.Handled = true;
        }
        else
            _popup.PopupEntity(Loc.GetString(component._fail), args.Performer, args.Performer);
    }

    private bool IsValidTile(EntityCoordinates coords, EntityWhitelist? whitelist, EntityWhitelist? blacklist, EntityUid grid, MapGridComponent gridComp)
    {
        // Only place webs on webs
        if (whitelist != null)
        {
            foreach (var entity in _lookup.GetEntitiesIntersecting(coords, LookupFlags.Uncontained))
                if (_whitelistSystem.IsWhitelistPass(whitelist, entity))
                    return true;
            
            return false;
        }

        // Don't place webs on webs
        if (blacklist != null)
            foreach (var entity in _lookup.GetEntitiesIntersecting(coords, LookupFlags.Uncontained))
                if (_whitelistSystem.IsBlacklistPass(blacklist, entity))
                    return false;

        // Don't place webs in space
        if (!_map.TryGetTileRef(grid, gridComp, coords, out var tileRef) ||
            tileRef.IsSpace(_tile))
                return false;

        return true;
    }
}
