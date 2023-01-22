using System.Threading;
using Content.Server.Administration.Logs;
using Content.Server.Maps;
using Content.Server.Tools.Components;
using Content.Shared.Database;
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
        SubscribeLocalEvent<LatticeCuttingComponent, LatticeCuttingCancelledEvent>(OnLatticeCutCancelled);
    }

    private void OnLatticeCutCancelled(EntityUid uid, LatticeCuttingComponent component, LatticeCuttingCancelledEvent args)
    {
        component.CancelTokenSource = null;
    }

    private void OnLatticeCutComplete(EntityUid uid, LatticeCuttingComponent component, LatticeCuttingCompleteEvent args)
    {
        component.CancelTokenSource = null;
        var gridUid = args.Coordinates.GetGridUid(EntityManager);
        if (gridUid == null)
            return;
        var grid = _mapManager.GetGrid(gridUid.Value);
        var tile = grid.GetTileRef(args.Coordinates);

        if (_tileDefinitionManager[tile.Tile.TypeId] is not ContentTileDefinition tileDef
            || !tileDef.CanWirecutter
            || tileDef.BaseTurfs.Count == 0
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

        if (TryCut(args.User, component, args.ClickLocation))
            args.Handled = true;
    }

    private bool TryCut(EntityUid user, LatticeCuttingComponent component, EntityCoordinates clickLocation)
    {
        if (component.CancelTokenSource != null)
            return true;

        ToolComponent? tool = null;
        if (component.ToolComponentNeeded && !TryComp<ToolComponent?>(component.Owner, out tool))
            return false;

        if (!_mapManager.TryGetGrid(clickLocation.GetGridUid(EntityManager), out var mapGrid))
            return false;

        var tile = mapGrid.GetTileRef(clickLocation);

        var coordinates = mapGrid.GridTileToLocal(tile.GridIndices);

        if (!_interactionSystem.InRangeUnobstructed(user, coordinates, popup: false))
            return false;

        if (_tileDefinitionManager[tile.Tile.TypeId] is not ContentTileDefinition tileDef
            || !tileDef.CanWirecutter
            || tileDef.BaseTurfs.Count == 0
            || _tileDefinitionManager[tileDef.BaseTurfs[^1]] is not ContentTileDefinition newDef
            || tile.IsBlockedTurf(true))
            return false;

        var tokenSource = new CancellationTokenSource();
        component.CancelTokenSource = tokenSource;

        if (!UseTool(component.Owner, user, null, 0f, component.Delay, new[] {component.QualityNeeded},
                new LatticeCuttingCompleteEvent
                {
                    Coordinates = clickLocation,
                    User = user
                }, new LatticeCuttingCancelledEvent(), toolComponent: tool, doAfterEventTarget: component.Owner,
                cancelToken: tokenSource.Token))
            component.CancelTokenSource = null;

        return true;
    }

    private sealed class LatticeCuttingCompleteEvent : EntityEventArgs
    {
        public EntityCoordinates Coordinates;
        public EntityUid User;
    }

    private sealed class LatticeCuttingCancelledEvent : EntityEventArgs
    {
    }
}

