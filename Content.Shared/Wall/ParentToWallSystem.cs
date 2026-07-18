using Content.Shared.Destructible;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Wall;

/// <summary>
/// A system for wallmounts to have their lifecycle tied with the wallmounted objects that live on them.
/// Ensures that wallmount entities aren't left floating in space when a wall is destroyed.
/// </summary>
public sealed partial class ParentToWallSystem : EntitySystem
{
    [Dependency] private SharedDestructibleSystem _destructible = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private TagSystem _tag = default!;

    [Dependency] private EntityQuery<MapGridComponent> _mapGridQuery = default!;
    [Dependency] private EntityQuery<ParentedWallComponent> _parentedWallQuery = default!;

    private static readonly ProtoId<TagPrototype> WallTag = "Wall";

    /// <summary>
    /// Wallmount init: tries to find a wall to parent itself to and registers itself.
    /// </summary>
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
            if (maybeAnchor is not { } anchor || !_tag.HasTag(anchor, WallTag))
                continue;

            // Parent the entity to the wall.
            var parentedWall = EnsureComp<ParentedWallComponent>(anchor);
            parentedWall.Children.Add(ent);
            Dirty(anchor, parentedWall);

            ent.Comp.Parent = anchor;
            Dirty(ent);

            return;
        }
    }

    /// <summary>
    /// Wallmount remove handler: removes the entity from its linked parent's set.
    /// </summary>
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

    /// <summary>
    /// Wall destroyed handler: delete all the wall's linked children, trying to destroy them first.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnParentedWallDestroyed(Entity<ParentedWallComponent> ent, ref DestructionEventArgs args)
    {
        DeleteChildren(ent, attemptDestroy: true);
    }

    /// <summary>
    /// Wall termining handler: delete all the wall's linked children.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnParentedWallTerminating(Entity<ParentedWallComponent> ent, ref EntityTerminatingEvent args)
    {
        DeleteChildren(ent, attemptDestroy: false);
    }

    /// <summary>
    /// Destroys all of an entities linked entities, optionally attempting to destroy them.
    /// </summary>
    private void DeleteChildren(Entity<ParentedWallComponent> ent, bool attemptDestroy)
    {
        foreach (var child in ent.Comp.Children)
        {
            // Already being destroyed, nothing to do.
            if (TerminatingOrDeleted(child))
                continue;

            // Try to destroy the entity normally, otherwise queue delete it
            if (attemptDestroy && _destructible.DestroyEntity(child))
                continue;

            PredictedQueueDel(child);
        }
    }
}
