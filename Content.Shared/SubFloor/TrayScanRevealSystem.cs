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

    private void OnStartup(EntityUid uid, TrayScanRevealComponent component, EntityEventArgs _)
    {
        var gridUid = _transform.GetGrid(uid);
        if (gridUid is null)
            return;

        var gridComp = Comp<MapGridComponent>(gridUid.Value);
        var position = _transform.GetGridOrMapTilePosition(uid);

        (component.TileGridUid, component.TileGridComp, component.TileIndices) = ((EntityUid)gridUid, gridComp, position);
        _subFloorHide.UpdateTile((EntityUid)gridUid, gridComp, position);
    }

    private void OnRemove(EntityUid uid, TrayScanRevealComponent component, EntityEventArgs _)
    {
        if (component.TileGridUid != EntityUid.Invalid)
            _subFloorHide.UpdateTile(component.TileGridUid, component.TileGridComp, component.TileIndices);
    }

    internal bool HasTrayScanReveal(Entity<MapGridComponent> ent, Vector2i position)
    {
        var anchoredEnum = _map.GetAnchoredEntities(ent, position);
        return anchoredEnum.Any(HasComp<TrayScanRevealComponent>);
    }
}
