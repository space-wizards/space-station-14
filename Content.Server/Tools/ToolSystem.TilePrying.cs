using System;
using System.Threading;
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
        SubscribeLocalEvent<TilePryingComponent, TilePryingCompleteEvent>(OnTilePryComplete);
    }

    private void OnTilePryComplete(EntityUid uid, TilePryingComponent component, TilePryingCompleteEvent args)
    {
        component.CancelToken = null;
        args.Coordinates.PryTile(EntityManager, _mapManager);
    }

    private void OnTilePryingAfterInteract(EntityUid uid, TilePryingComponent component, AfterInteractEvent args)
    {
        if (args.Handled) return;

        if (TryPryTile(args.User, component, args.ClickLocation))
            args.Handled = true;
    }

    private bool TryPryTile(EntityUid user, TilePryingComponent component, EntityCoordinates clickLocation)
    {
        if (component.CancelToken != null)
        {
            component.CancelToken.Cancel();
            component.CancelToken = null;
            return false;
        }

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

        var token = new CancellationTokenSource();
        component.CancelToken = token;

        UseTool(
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
            toolComponent: tool,
            doAfterEventTarget: component.Owner,
            cancelToken: token.Token);

        return true;
    }

    private sealed class TilePryingCompleteEvent : EntityEventArgs
    {
        public EntityCoordinates Coordinates { get; init; }
    }
}
