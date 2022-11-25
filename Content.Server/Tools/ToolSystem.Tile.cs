using System.Threading;
using Content.Server.Fluids.Components;
using Content.Server.Tools.Components;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Tools.Components;
using Content.Shared.Database;
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

        if (tileRef.IsBlockedTurf(true))
            return;
        
        if (tileRef.TryDeconstructWithToolQualities(tool.Qualities, _mapManager, _tileDefinitionManager, EntityManager))
            _adminLogger.Add(LogType.TileEdited, LogImpact.Medium,
                $"{_entityManager.ToPrettyString(args.User):player} used {_entityManager.ToPrettyString(uid)} to edit the tile at {args.Coordinates}");
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

        var tileRef = mapGrid.GetTileRef(clickLocation);

        var coordinates = mapGrid.GridTileToLocal(tileRef.GridIndices);

        var tileDef = (ContentTileDefinition) _tileDefinitionManager[tileRef.Tile.TypeId];

        if (!tileDef.DeconstructToolQualities.ContainsAll(tool.Qualities))
            return false;

        if (tileRef.IsBlockedTurf(true))
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

    private sealed class TileToolCompleteEvent : EntityEventArgs
    {
        public EntityCoordinates Coordinates { get; init; }
        public EntityUid User { get; init; }
    }

    private sealed class TileToolCancelledEvent : EntityEventArgs
    {
    }
}
