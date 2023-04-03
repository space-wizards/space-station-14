using System.Threading;
using Content.Server.Fluids.Components;
using Content.Server.Tools.Components;
using Content.Shared.DoAfter;
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

        var gridUid = args.Coordinates.GetGridUid(EntityManager);
        if (!_mapManager.TryGetGrid(gridUid, out var grid))
        {
            Logger.Error("Attempted to pry from a non-existent grid?");
            return;
        }

        var tile = grid.GetTileRef(args.Coordinates);
        _tile.PryTile(tile);
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

        var ev = new TilePryingDoAfterEvent(clickLocation);

        return UseTool(toolEntity, user, toolEntity, component.Delay, component.QualityNeeded, ev, toolComponent: tool);
    }
}
