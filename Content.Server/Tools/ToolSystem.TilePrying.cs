using Content.Server.Tools.Components;
using Content.Shared.Database;
using Content.Shared.Fluids.Components;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Tools.Components;
using Robust.Shared.Map;

namespace Content.Server.Tools;

public sealed partial class ToolSystem
{
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

    private void InitializeTilePrying()
    {
        SubscribeLocalEvent<TilePryingComponent, AfterInteractEvent>(OnTilePryingAfterInteract);
        SubscribeLocalEvent<TilePryingComponent, TilePryingDoAfterEvent>(OnTilePryComplete);
    }

    private void OnTilePryingAfterInteract(EntityUid uid, TilePryingComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || (args.Target != null && !HasComp<PuddleComponent>(args.Target))) return;

        if (TryPryTile(uid, args.User, component, args.ClickLocation))
            args.Handled = true;
    }

    private void OnTilePryComplete(EntityUid uid, TilePryingComponent component, TilePryingDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        var coords = GetCoordinates(args.Coordinates);
        var gridUid = coords.GetGridUid(EntityManager);
        if (!_mapManager.TryGetGrid(gridUid, out var grid))
        {
            Log.Error("Attempted to pry from a non-existent grid?");
            return;
        }

        var tile = grid.GetTileRef(coords);
        var center = _turf.GetTileCenter(tile);
        if (args.Used != null)
        {
            _adminLogger.Add(LogType.Tile, LogImpact.Low,
                $"{ToPrettyString(args.User):actor} used {ToPrettyString(args.Used.Value):tool} to pry {_tileDefinitionManager[tile.Tile.TypeId].Name} at {center}");
        }
        else
        {
            _adminLogger.Add(LogType.Tile, LogImpact.Low,
                $"{ToPrettyString(args.User):actor} pried {_tileDefinitionManager[tile.Tile.TypeId].Name} at {center}");
        }

        _tile.PryTile(tile, component.Advanced);
    }

    private bool TryPryTile(EntityUid toolEntity, EntityUid user, TilePryingComponent component, EntityCoordinates clickLocation)
    {
        if (!TryComp<ToolComponent>(toolEntity, out var tool) && component.ToolComponentNeeded)
            return false;

        if (!_mapManager.TryFindGridAt(clickLocation.ToMap(EntityManager, _transformSystem), out _, out var mapGrid))
            return false;

        var tile = mapGrid.GetTileRef(clickLocation);

        var coordinates = mapGrid.GridTileToLocal(tile.GridIndices);

        if (!_interactionSystem.InRangeUnobstructed(user, coordinates, popup: false))
            return false;

        var tileDef = (ContentTileDefinition)_tileDefinitionManager[tile.Tile.TypeId];

        if (!tileDef.CanCrowbar && !(tileDef.CanAxe && component.Advanced))
            return false;

        var ev = new TilePryingDoAfterEvent(GetNetCoordinates(coordinates));

        return UseTool(toolEntity, user, toolEntity, component.Delay, component.QualityNeeded, ev, toolComponent: tool);
    }
}
