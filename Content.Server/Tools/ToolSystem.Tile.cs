using System.Threading;
using Content.Server.Fluids.Components;
using Content.Server.Tools.Components;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Tools.Components;
using Robust.Shared.Map;

namespace Content.Server.Tools;

public sealed partial class ToolSystem
{
    private void InitializeToolWithTile()
    {
        SubscribeLocalEvent<ToolWorksWithTilesComponent, AfterInteractEvent>(OnToolWithTileAfterInteract);
        SubscribeLocalEvent<ToolWorksWithTilesComponent, TileToolCompleteEvent>(OnToolWithTileComplete);
        SubscribeLocalEvent<ToolWorksWithTilesComponent, TileToolCancelledEvent>(OnToolWithTileCancelled);
    }

    private void OnToolWithTileCancelled(EntityUid uid, ToolWorksWithTilesComponent component, TileToolCancelledEvent args)
    {
        component.CancelTokenSource = null;
    }

    private void OnToolWithTileComplete(EntityUid uid, ToolWorksWithTilesComponent component, TileToolCompleteEvent args)
    {
        component.CancelTokenSource = null;
        var gridUid = args.Coordinates.GetGridUid(EntityManager);
        if (!_mapManager.TryGetGrid(gridUid, out var grid))
        {
            Logger.Error("Attempted use tool on a non-existent grid?");
            return;
        }

        if (!TryComp(component.Owner, out ToolComponent? tool))
            return;

        var tileRef = grid.GetTileRef(args.Coordinates);

        if (!CheckTileConditions(tileRef, tool.Qualities))
            return;
        
        tileRef.TryDeconstructWithToolQualities(tool.Qualities, _mapManager, _tileDefinitionManager, EntityManager);
        // TODO admin log esp cutting lattice
    }

    private void OnToolWithTileAfterInteract(EntityUid uid, ToolWorksWithTilesComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target.HasValue)
            return;
        
        if (UseToolOnTile(args.User, component, args.ClickLocation))
            args.Handled = true;
    }

    private bool UseToolOnTile(EntityUid user, ToolWorksWithTilesComponent component, EntityCoordinates clickLocation)
    {
        if (component.CancelTokenSource != null)
            return true;

        if (!TryComp<ToolComponent?>(component.Owner, out var tool))
            return false;

        if (!_mapManager.TryGetGrid(clickLocation.GetGridUid(EntityManager), out var mapGrid))
            return false;

        var tile = mapGrid.GetTileRef(clickLocation);

        var coordinates = mapGrid.GridTileToLocal(tile.GridIndices);

        //if (!_interactionSystem.InRangeUnobstructed(user, coordinates, popup: false))
            //return false;

        var tileDef = (ContentTileDefinition) _tileDefinitionManager[tile.Tile.TypeId];

        if (!tileDef.DeconstructToolQualities.ContainsAll(tool.Qualities))
            return false;

        var tokenSource = new CancellationTokenSource();
        component.CancelTokenSource = tokenSource;

        bool success = UseTool(
            component.Owner,
            user,
            null,
            0f,
            component.Delay,
            tool.Qualities,
            new TileToolCompleteEvent
            {
                Coordinates = clickLocation,
                User = user,
            },
            new TileToolCancelledEvent(),
            toolComponent: tool,
            doAfterEventTarget: component.Owner,
            cancelToken: tokenSource.Token);

        if (!success)
            component.CancelTokenSource = null;

        return true;
    }

    private bool CheckTileConditions(TileRef tileRef, IEnumerable<string> toolQualities)
    {
        if (_tileDefinitionManager[tileRef.Tile.TypeId] is ContentTileDefinition tileDef)
        {
            if (tileDef.DeconstructToolQualities.ContainsAll(toolQualities)
                && !string.IsNullOrEmpty(tileDef.BaseTurf)
                && !tileRef.IsBlockedTurf(true))
                return true;
        }
        return false;
    }

    private sealed class TileToolCompleteEvent : EntityEventArgs
    {
        public EntityCoordinates Coordinates { get; init; }
        public EntityUid User { get; init; }
    }

    private sealed class TileToolCancelledEvent : EntityEventArgs
    {
    }
}
