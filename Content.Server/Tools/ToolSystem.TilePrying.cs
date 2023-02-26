using System.Threading;
using Content.Server.Fluids.Components;
using Content.Server.Tools.Components;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Tools.Components;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Tools;

public sealed partial class ToolSystem
{
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;

    private void InitializeTilePrying()
    {
        SubscribeLocalEvent<TilePryingComponent, AfterInteractEvent>(OnTilePryingAfterInteract);
        SubscribeLocalEvent<TilePryingComponent, TilePryingCompleteEvent>(OnTilePryComplete);
    }

    private void OnTilePryComplete(EntityUid uid, TilePryingComponent component, TilePryingCompleteEvent args)
    {
        var gridUid = args.Coordinates.GetGridUid(EntityManager);
        if (!_mapManager.TryGetGrid(gridUid, out var grid))
        {
            Logger.Error("Attempted to pry from a non-existent grid?");
            return;
        }

        var tile = grid.GetTileRef(args.Coordinates);
        _tile.PryTile(tile);
    }

    private void OnTilePryingAfterInteract(EntityUid uid, TilePryingComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || (args.Target != null && !HasComp<PuddleComponent>(args.Target))) return;

        if (TryPryTile(uid, args.User, component, args.ClickLocation))
            args.Handled = true;
    }

    private bool TryPryTile(EntityUid toolEntity, EntityUid user, TilePryingComponent component, EntityCoordinates clickLocation)
    {
        if (!TryComp<ToolComponent?>(toolEntity, out var tool) && component.ToolComponentNeeded)
            return false;

        if (!_mapManager.TryGetGrid(clickLocation.GetGridUid(EntityManager), out var mapGrid))
            return false;

        var tile = mapGrid.GetTileRef(clickLocation);

        var coordinates = mapGrid.GridTileToLocal(tile.GridIndices);

        if (!_interactionSystem.InRangeUnobstructed(user, coordinates, popup: false))
            return false;

        var tileDef = (ContentTileDefinition)_tileDefinitionManager[tile.Tile.TypeId];

        if (!tileDef.CanCrowbar)
            return false;

        var toolEvData = new ToolEventData(new TilePryingCompleteEvent(clickLocation), targetEntity:toolEntity);

        if (!UseTool(toolEntity, user, null, component.Delay, new[] { component.QualityNeeded }, toolEvData, toolComponent: tool))
            return false;

        return true;
    }

    private sealed class TilePryingCompleteEvent : EntityEventArgs
    {
        public readonly EntityCoordinates Coordinates;

        public TilePryingCompleteEvent(EntityCoordinates coordinates)
        {
            Coordinates = coordinates;
        }
    }

    private sealed class TilePryingCancelledEvent : EntityEventArgs
    {

    }
}
