using Content.Server.Tools.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Maps;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.Server.Tools;

public sealed partial class ToolSystem
{
    private void InitializeTilePrying()
    {
        SubscribeLocalEvent<TilePryingComponent, AfterInteractEvent>(OnTilePryingAfterInteract);
        SubscribeLocalEvent<TilePryingCompleteEvent>(OnTilePryComplete);
        SubscribeLocalEvent<TilePryingCancelledEvent>(OnTilePryCancelled);
    }

    private void OnTilePryComplete(TilePryingCompleteEvent ev)
    {
        ev.Component.Prying = false;
        ev.Coordinates.PryTile(EntityManager, _mapManager);
    }

    private static void OnTilePryCancelled(TilePryingCancelledEvent ev)
    {
        ev.Component.Prying = false;
    }

    private void OnTilePryingAfterInteract(EntityUid uid, TilePryingComponent component, AfterInteractEvent args)
    {
        if (args.Handled) return;

        if (TryPryTile(args.User, component, args.ClickLocation))
            args.Handled = true;
    }

    private bool TryPryTile(EntityUid user, TilePryingComponent component, EntityCoordinates clickLocation)
    {
        if (!TryComp<ToolComponent?>(component.Owner, out var tool) && component.ToolComponentNeeded)
            return false;

        if (!_mapManager.TryGetGrid(clickLocation.GetGridId(EntityManager), out var mapGrid))
            return false;

        var tile = mapGrid.GetTileRef(clickLocation);

        var coordinates = mapGrid.GridTileToLocal(tile.GridIndices);

        if (!user.InRangeUnobstructed(coordinates, popup: false))
            return false;

        var tileDef = (ContentTileDefinition)_tileDefinitionManager[tile.Tile.TypeId];

        if (!tileDef.CanCrowbar)
            return false;

        if (component.Prying)
            return true;

        // Should tools be unique per UseTool? Future concern
        component.Prying = true;

        UseTool(
            component.Owner,
            user,
            null,
            0f,
            component.Delay,
            new [] {component.QualityNeeded},
            new TilePryingCompleteEvent
            {
                Component = component,
                Coordinates = clickLocation,
            },
            new TilePryingCancelledEvent
            {
                Component = component,
            },
            toolComponent: tool);

        return true;
    }

    private sealed class TilePryingCancelledEvent : EntityEventArgs
    {
        public TilePryingComponent Component { get; init; } = default!;
    }

    private sealed class TilePryingCompleteEvent : EntityEventArgs
    {
        public TilePryingComponent Component { get; init; } = default!;
        public EntityCoordinates Coordinates { get; init; }
    }
}
