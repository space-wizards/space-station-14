using System.Linq;
using Robust.Shared.Map.Components;

namespace Content.Shared.SubFloor;

public sealed class TrayScanRevealSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedSubFloorHideSystem _subFloorHide = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TrayScanRevealComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<TrayScanRevealComponent, ComponentRemove>(OnRemove);
    }

    private void OnStartup(EntityUid uid, TrayScanRevealComponent component, EntityEventArgs args)
    {
        var gridUid = _transform.GetGrid(uid);
        if (gridUid is null)
            return;

        var gridComp = Comp<MapGridComponent>(gridUid.Value);
        var position = _transform.GetGridOrMapTilePosition(uid);

        component.Tile = ((EntityUid)gridUid, gridComp, position);
        _subFloorHide.UpdateTile((EntityUid)gridUid, gridComp, position);
    }

    private void OnRemove(EntityUid uid, TrayScanRevealComponent component, EntityEventArgs args)
    {
        _subFloorHide.UpdateTile(component.Tile.Item1, component.Tile.Item2, component.Tile.Item3);
    }

    internal bool HasTrayScanReveal(EntityUid gridUid, MapGridComponent grid, Vector2i position)
    {
        var anchoredEnum = _map.GetAnchoredEntities(gridUid, grid, position);
        return anchoredEnum.Any(HasComp<TrayScanRevealComponent>);
    }

    internal bool HasEntityTileTrayScanReveal(EntityUid uid, TransformComponent? xform)
    {
        if (!Resolve(uid, ref xform))
            return false;

        var gridUid = xform.GridUid;
        if (gridUid is null)
            return false;

        var gridComp = Comp<MapGridComponent>(gridUid.Value);
        var position = (Vector2i)xform.LocalPosition;

        return HasTrayScanReveal((EntityUid)gridUid, gridComp, position);
    }
}
