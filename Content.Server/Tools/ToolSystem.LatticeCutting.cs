using Content.Server.Administration.Logs;
using Content.Server.Maps;
using Content.Server.Tools.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Tools.Components;
using Robust.Shared.Map;

namespace Content.Server.Tools;

public sealed partial class ToolSystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly TileSystem _tile = default!;

    private void InitializeLatticeCutting()
    {
        SubscribeLocalEvent<LatticeCuttingComponent, AfterInteractEvent>(OnLatticeCuttingAfterInteract);
        SubscribeLocalEvent<LatticeCuttingComponent, LatticeCuttingCompleteEvent>(OnLatticeCutComplete);
    }

    private void OnLatticeCutComplete(EntityUid uid, LatticeCuttingComponent component, LatticeCuttingCompleteEvent args)
    {
        if (args.Cancelled)
            return;

        var coords = GetCoordinates(args.Coordinates);
        var gridUid = coords.GetGridUid(EntityManager);
        if (gridUid == null)
            return;
        var grid = _mapManager.GetGrid(gridUid.Value);
        var tile = grid.GetTileRef(coords);

        if (_tileDefinitionManager[tile.Tile.TypeId] is not ContentTileDefinition tileDef
            || !tileDef.CanWirecutter
            || string.IsNullOrEmpty(tileDef.BaseTurf)
            || tile.IsBlockedTurf(true))
            return;

        _tile.CutTile(tile);
        _adminLogger.Add(LogType.LatticeCut, LogImpact.Medium, $"{ToPrettyString(args.User):user} cut the lattice at {args.Coordinates:target}");
    }

    private void OnLatticeCuttingAfterInteract(EntityUid uid, LatticeCuttingComponent component,
        AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target != null)
            return;

        if (TryCut(uid, args.User, component, args.ClickLocation))
            args.Handled = true;
    }

    private bool TryCut(EntityUid toolEntity, EntityUid user, LatticeCuttingComponent component, EntityCoordinates clickLocation)
    {
        if (!_mapManager.TryFindGridAt(clickLocation.ToMap(EntityManager, _transformSystem), out _, out var mapGrid))
            return false;

        var tile = mapGrid.GetTileRef(clickLocation);

        var coordinates = mapGrid.GridTileToLocal(tile.GridIndices);

        if (!_interactionSystem.InRangeUnobstructed(user, coordinates, popup: false))
            return false;

        if (_tileDefinitionManager[tile.Tile.TypeId] is not ContentTileDefinition tileDef
            || !tileDef.CanWirecutter
            || string.IsNullOrEmpty(tileDef.BaseTurf)
            || _tileDefinitionManager[tileDef.BaseTurf] is not ContentTileDefinition newDef
            || tile.IsBlockedTurf(true))
        {
            return false;
        }

        var ev = new LatticeCuttingCompleteEvent(GetNetCoordinates(coordinates));
        return UseTool(toolEntity, user, toolEntity, component.Delay, component.QualityNeeded, ev);
    }
}

