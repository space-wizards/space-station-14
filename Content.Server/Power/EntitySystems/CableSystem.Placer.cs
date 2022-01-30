using Content.Server.Power.Components;
using Content.Server.Stack;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Maps;
using Robust.Shared.GameObjects;

namespace Content.Server.Power.EntitySystems;

public sealed partial class CableSystem
{
    private void InitializeCablePlacer()
    {
        SubscribeLocalEvent<CablePlacerComponent, AfterInteractEvent>(OnCablePlacerAfterInteract);
    }

    private void OnCablePlacerAfterInteract(EntityUid uid, CablePlacerComponent component, AfterInteractEvent args)
    {
        if (args.Handled) return;

        if (component.CablePrototypeId == null) return;

        if (!args.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true))
            return;

        if(!_mapManager.TryGetGrid(args.ClickLocation.GetGridId(EntityManager), out var grid))
            return;

        var snapPos = grid.TileIndicesFor(args.ClickLocation);
        var tileDef = (ContentTileDefinition) _tileManager[grid.GetTileRef(snapPos).Tile.TypeId];

        if (!tileDef.IsSubFloor || !tileDef.Sturdy)
            return;

        foreach (var anchored in grid.GetAnchoredEntities(snapPos))
        {
            if (TryComp<CableComponent>(anchored, out var wire) && wire.CableType == component.BlockingCableType)
                return;
        }

        if (TryComp<StackComponent>(component.Owner, out var stack) && !_stack.Use(component.Owner, 1, stack))
            return;

        EntityManager.SpawnEntity(component.CablePrototypeId, grid.GridTileToLocal(snapPos));
        args.Handled = true;
    }
}
