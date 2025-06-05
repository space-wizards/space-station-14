using System.Linq;
using Content.Shared.SubFloor;
using Robust.Shared.Map.Components;

namespace Content.Client.SubFloor;

public sealed class TrayScanRevealSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    public bool IsUnderRevealingEntity(EntityUid uid)
    {
        var gridUid = _transform.GetGrid(uid);
        if (gridUid is null)
            return false;

        var gridComp = Comp<MapGridComponent>(gridUid.Value);
        var position = _transform.GetGridOrMapTilePosition(uid);

        return HasTrayScanReveal(((EntityUid)gridUid, gridComp), position);
    }

    private bool HasTrayScanReveal(Entity<MapGridComponent> ent, Vector2i position)
    {
        var anchoredEnum = _map.GetAnchoredEntities(ent, position);
        return anchoredEnum.Any(HasComp<TrayScanRevealComponent>);
    }
}
