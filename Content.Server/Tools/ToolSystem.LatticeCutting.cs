using System.Threading;
using Content.Server.Tools.Components;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.Tools;

public sealed partial class ToolSystem
{
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
        var gridUid = Transform(uid).GridUid;
        if (gridUid == null)
            return;
        var grid = _mapManager.GetGrid(gridUid.Value);
        var tile = grid.GetTileRef(args.Coordinates);
        var snapPos = grid.TileIndicesFor(args.Coordinates);

        if (_tileDefinitionManager[tile.Tile.TypeId] is not ContentTileDefinition tileDef
            || tileDef.BaseTurfs.Count == 0
            || _tileDefinitionManager[tileDef.BaseTurfs[^1]] is not ContentTileDefinition newDef
            || tile.IsBlockedTurf(true))
            return;

        grid.SetTile(snapPos, new Tile(newDef.TileId));
    }

    private void OnLatticeCuttingAfterInteract(EntityUid uid, LatticeCuttingComponent component,
        AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (TryCut(args.User, component, args.ClickLocation))
            args.Handled = true;
    }

    private bool TryCut(EntityUid user, LatticeCuttingComponent component, EntityCoordinates  clickLocation)
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
            || tileDef.BaseTurfs.Count == 0
            || _tileDefinitionManager[tileDef.BaseTurfs[^1]] is not ContentTileDefinition newDef
            || tile.IsBlockedTurf(true))
            return false;

        if (tileDef.ID != "Plating" && tileDef.ID != "Lattice")
            return false;

        var tokenSource = new CancellationTokenSource();
        component.CancelTokenSource = tokenSource;

        var delay = component.Delay;
        if (!tileDef.IsSpace && newDef.IsSpace) // TODO Add atmos decompression check
            delay += component.VacuumDelay;
            _popupSystem.PopupCursor(Loc.GetString("comp-lattice-cutting-unsafe-warning"), Filter.Entities(user), PopupType.MediumCaution);

        if (UseTool(component.Owner, user, null, 0f, delay, new[] {component.QualityNeeded},
                new LatticeCuttingCompleteEvent
                {
                    Coordinates = clickLocation
                }, new LatticeCuttingCancelledEvent(), toolComponent: tool, doAfterEventTarget: component.Owner,
                cancelToken: tokenSource.Token))
            component.CancelTokenSource = null;

        return true;
    }

    private sealed class LatticeCuttingCompleteEvent : EntityEventArgs
    {
        public EntityCoordinates Coordinates { get; set; }
    }

    private sealed class LatticeCuttingCancelledEvent : EntityEventArgs
    {
    }
}

