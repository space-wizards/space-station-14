using Content.Shared.Destructible;
using Content.Shared.RCD;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.Wall;

/// <summary>
/// A system for wallmounts to have their lifecycle tied to the wall that they're mounted on.
/// Ensures that wallmount entities aren't left floating in space when a wall is destroyed.
/// </summary>
public sealed partial class ParentToWallSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedDestructibleSystem _destructible = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private TagSystem _tag = default!;

    [Dependency] private EntityQuery<MapGridComponent> _mapGridQuery = default!;
    [Dependency] private EntityQuery<ParentedWallComponent> _parentedWallQuery = default!;
    [Dependency] private EntityQuery<ParentToWallComponent> _childWallmountQuery = default!;

    private static readonly ProtoId<TagPrototype> WallTag = "Wall";

    #region Handlers
    /// <summary>
    /// Wallmount init: tries to find a wall to parent itself to and registers itself.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnParentToWallMapInit(Entity<ParentToWallComponent> ent, ref MapInitEvent args)
    {
        if (Transform(ent).GridUid is not { } gridUid
            || !_mapGridQuery.TryComp(gridUid, out var mapGrid))
            return;

        // Find our target position relative to our entity.
        var coords = new EntityCoordinates(ent, ent.Comp.Offset);

        if (!_map.TryGetTileRef(gridUid, mapGrid, coords, out var tileRef))
            return;

        // Look for an anchored wall by its tag.
        var anchoredQuery = _map.GetAnchoredEntitiesEnumerator(gridUid, mapGrid, tileRef.GridIndices);
        while (anchoredQuery.MoveNext(out var maybeAnchor))
        {
            if (maybeAnchor is not { } anchor || !_tag.HasTag(anchor, WallTag))
                continue;

            // Parent the entity to the wall.
            var parentedWall = EnsureComp<ParentedWallComponent>(anchor);
            if (!parentedWall.Children.Contains(ent))
            {
                parentedWall.Children.Add(ent);
                Dirty(anchor, parentedWall);
            }

            if (!ent.Comp.Anchor)
            {
                ent.Comp.Anchored = false;
                _transform.SetParent(ent, anchor);
            }

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
        DetachFromParent(ent);
    }

    /// <summary>
    /// Wallmount terminating handler: removes the entity from its linked parent's set.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnParentToWallTerminating(Entity<ParentToWallComponent> ent, ref EntityTerminatingEvent args)
    {
        DetachFromParent(ent);
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
    /// Handles the anchor state of our wall changing.
    /// We can only anchor entities to grids so we need to reparent all child entities when our wall anchoring changes.
    /// GODO...
    /// </summary>
    /// <param name="ent">The wall being unanchored.</param>
    /// <param name="args">The event in question.</param>
    [SubscribeLocalEvent]
    private void OnWallAnchorChanged(Entity<ParentedWallComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored)
        {
            foreach (var child in ent.Comp.Children)
            {
                // FIXME: load-bearing cope - client is full of invalid entities
                if (TerminatingOrDeleted(child))
                {
                    Log.Warning($"Child {child} is terminating in {ent}");
                    continue;
                }

                if (_childWallmountQuery.TryComp(child, out var parentToWall)
                    && parentToWall.Anchored)
                {
                    parentToWall.Anchored = false;
                    Dirty(child, parentToWall);
                }

                _transform.SetParent(child, ent);
            }
        }
        else
        {
            foreach (var child in ent.Comp.Children)
            {
                // FIXME: load-bearing cope - client is full of invalid entities
                if (TerminatingOrDeleted(child))
                {
                    Log.Warning($"Child {child} is terminating in {ent}");
                    continue;
                }

                // Only reanchor if the child wants to be anchored.
                if (_childWallmountQuery.TryComp(child, out var parentToWall))
                {
                    if (!parentToWall.Anchor)
                        continue;

                    if (!parentToWall.Anchored)
                    {
                        parentToWall.Anchored = true;
                        Dirty(child, parentToWall);
                    }
                }

                var childXform = Transform(child);
                if (!childXform.Anchored)
                    childXform.Anchored = true; // FIXME: TransformSystem.AnchorEntity doesn't play well with uninitialized entities, see RT#6739.
            }
        }
    }

    /// <summary>
    /// Handles the anchor state of a wallmount changing.
    /// If we weren't expecting this from our own
    /// </summary>
    /// <param name="ent">The wall being unanchored.</param>
    /// <param name="args">The event in question.</param>
    [SubscribeLocalEvent]
    private void OnChildAnchorChanged(Entity<ParentToWallComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        // We've been unexpectedly (un)anchored, farewell parent.
        if (args.Anchored != ent.Comp.Anchored)
            DetachFromParent(ent);
    }

    /// <summary>
    /// Handles RCD deconstruction on a wall.
    /// If there is anything that should block deconstruction, cancel the attempt and tell the user why.
    /// </summary>
    /// <param name="ent">The wall being deconstructed.</param>
    /// <param name="args">The event in question.</param>
    [SubscribeLocalEvent]
    private void OnAttemptRCDDeconstruction(Entity<ParentedWallComponent> ent, ref AttemptRCDDeconstructionEvent args)
    {
        if (args.Cancelled)
            return;

        foreach (var child in ent.Comp.Children)
        {
            if (!_childWallmountQuery.TryComp(child, out var parentToWall)
                || !parentToWall.BlockDeconstruction)
                continue;

            args.Reason = Loc.GetString("parent-to-wall-cannot-deconstruct");
            args.Cancel();
            return;
        }
    }
    #endregion Handlers

    #region Internal
    /// <summary>
    /// Destroys all of a wall's linked entities, optionally attempting to destroy them.
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
        ent.Comp.Children.Clear();
    }

    /// <summary>
    /// Removes the association from a child to its wall, if it has one.
    /// </summary>
    private void DetachFromParent(Entity<ParentToWallComponent> ent)
    {
        // If this entity is being torn down by the parent, don't bookkeep.
        if (ent.Comp.Parent is not { } parent
            || TerminatingOrDeleted(parent)
            || !_parentedWallQuery.TryComp(ent.Comp.Parent, out var parentComp))
            return;

        if (parentComp.Children.Remove(ent))
            Dirty(parent, parentComp);
    }
    #endregion Internal
}
