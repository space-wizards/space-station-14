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
        SubscribeLocalEvent<TilePryingComponent, TilePryingCancelledEvent>(OnTilePryCancelled);
    }

    private void OnTilePryCancelled(EntityUid uid, TilePryingComponent component, TilePryingCancelledEvent args)
    {
        component.CancelToken = null;
    }

    private void OnTilePryComplete(EntityUid uid, TilePryingComponent component, TilePryingCompleteEvent args)
    {
        component.CancelToken = null;
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

        if (TryPryTile(args.User, component, args.ClickLocation))
            args.Handled = true;
    }

    private bool TryPryTile(EntityUid user, TilePryingComponent component, EntityCoordinates clickLocation)
    {
        if (component.CancelToken != null)
        {
            return true;
        }

        if (!TryComp<ToolComponent?>(component.Owner, out var tool) && component.ToolComponentNeeded)
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

        var token = new CancellationTokenSource();
        component.CancelToken = token;

        bool success = UseTool(
            component.Owner,
            user,
            null,
            0f,
            component.Delay,
            new [] {component.QualityNeeded},
            new TilePryingCompleteEvent
            {
                Coordinates = clickLocation,
            },
            new TilePryingCancelledEvent(),
            toolComponent: tool,
            doAfterEventTarget: component.Owner,
            cancelToken: token.Token);

        if (!success)
            component.CancelToken = null;

        return true;
    }

    private sealed class TilePryingCompleteEvent : EntityEventArgs
    {
        public EntityCoordinates Coordinates { get; init; }
    }

    private sealed class TilePryingCancelledEvent : EntityEventArgs
    {

    }
}
