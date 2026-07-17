using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Wall;

public sealed partial class ParentToWallSystem : EntitySystem
{
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private TagSystem _tag = default!;

    [Dependency] private EntityQuery<MapGridComponent> _mapGridQuery = default!;
    [Dependency] private EntityQuery<ParentedWallComponent> _parentedWallQuery = default!;

    private readonly ProtoId<TagPrototype> _wallTag = "Wall";

    [SubscribeLocalEvent]
    private void OnParentToWallMapInit(Entity<ParentToWallComponent> ent, ref MapInitEvent args)
    {
        if (Transform(ent).GridUid is not { } gridUid
            || !_mapGridQuery.TryComp(gridUid, out var mapGrid))
            return;

        var coords = new EntityCoordinates(ent, ent.Comp.Offset);

        if (!_map.TryGetTileRef(gridUid, mapGrid, coords, out var tileRef))
            return;

        var anchoredQuery = _map.GetAnchoredEntitiesEnumerator(gridUid, mapGrid, tileRef.GridIndices);
        while (anchoredQuery.MoveNext(out var maybeAnchor))
        {
            if (maybeAnchor is { } anchor && _tag.HasTag(anchor, _wallTag))
            {
                // Parent the entity to the wall.
                var parentedWall = EnsureComp<ParentedWallComponent>(anchor);
                parentedWall.Children.Add(ent);
                Dirty(anchor, parentedWall);

                ent.Comp.Parent = anchor;
                Dirty(ent);

                return;
            }
        }
    }

    [SubscribeLocalEvent]
    private void OnParentToWallRemove(Entity<ParentToWallComponent> ent, ref ComponentRemove args)
    {
        // If this entity is being torn down by the parent, don't bookkeep.
        if (ent.Comp.Parent is not { } parent
            || TerminatingOrDeleted(parent)
            || !_parentedWallQuery.TryComp(ent.Comp.Parent, out var parentComp))
            return;

        if (parentComp.Children.Remove(ent))
            Dirty(parent, parentComp);
    }

    [SubscribeLocalEvent]
    private void OnParentedWallDestroyed(Entity<ParentedWallComponent> ent, ref EntityTerminatingEvent args)
    {
        var children = ent.Comp.Children;
        foreach (var child in children)
        {
            if (!TerminatingOrDeleted(child))
                Del(child);
        }
    }
}
