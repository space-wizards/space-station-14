using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Tabletop.Components;
using Content.Shared.Tabletop.Events;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Tabletop;

public abstract class SharedTabletopSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly SharedTransformSystem Transforms = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    public override void Initialize()
    {
        SubscribeAllEvent<TabletopDraggingPlayerChangedEvent>(OnDraggingPlayerChanged);
        SubscribeAllEvent<TabletopMoveEvent>(OnTabletopMove);
    }

    /// <summary>
    ///     Move an entity which is dragged by the user, but check if they are allowed to do so and to these coordinates
    /// </summary>
    protected virtual void OnTabletopMove(TabletopMoveEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession is not { AttachedEntity: { } playerEntity })
            return;

        var table = GetEntity(msg.TableUid);
        var moved = GetEntity(msg.MovedEntityUid);

        if (!CanSeeTable(playerEntity, table) || !CanDrag(playerEntity, moved, out _))
            return;

        // Move the entity and dirty it (we use the map ID from the entity so noone can try to be funny and move the item to another map)
        var transform = EntityManager.GetComponent<TransformComponent>(moved);
        Transforms.SetParent(moved, transform, _map.GetMap(transform.MapID));
        Transforms.SetLocalPositionNoLerp(moved, msg.Coordinates.Position, transform);
    }

    private void OnDraggingPlayerChanged(TabletopDraggingPlayerChangedEvent msg, EntitySessionEventArgs args)
    {
        var dragged = GetEntity(msg.DraggedEntityUid);

        if (!TryComp<TabletopDraggableComponent>(dragged, out var draggableComponent))
            return;

        draggableComponent.DraggingPlayer = msg.IsDragging ? args.SenderSession.UserId : null;
        Dirty(dragged, draggableComponent);

        if (TryComp<AppearanceComponent>(dragged, out _))
            _appearance.SetData(dragged, TabletopItemVisuals.BeingDragged, draggableComponent.DraggingPlayer != null);
    }


    [Serializable, NetSerializable]
    public sealed class TabletopDraggableComponentState(NetUserId? draggingPlayer) : ComponentState
    {
        public NetUserId? DraggingPlayer = draggingPlayer;
    }

    [Serializable, NetSerializable]
    public sealed class TabletopRequestTakeOut : EntityEventArgs
    {
        public NetEntity Entity;
        public NetEntity TableUid;
    }

    #region Utility

    /// <summary>
    /// Whether the table exists, and the player can interact with it.
    /// </summary>
    /// <param name="playerEntity">The player entity to check.</param>
    /// <param name="table">The table entity to check.</param>
    protected bool CanSeeTable(EntityUid playerEntity, EntityUid? table)
    {
        // Table may have been deleted, hence TryComp
        if (!TryComp(table, out MetaDataComponent? meta)
            || meta.EntityLifeStage >= EntityLifeStage.Terminating
            || (meta.Flags & MetaDataFlags.InContainer) == MetaDataFlags.InContainer)
        {
            return false;
        }

        return _interactionSystem.InRangeUnobstructed(playerEntity, table.Value) &&
               _actionBlockerSystem.CanInteract(playerEntity, table);
    }

    protected bool CanDrag(EntityUid playerEntity,
        EntityUid target,
        [NotNullWhen(true)] out TabletopDraggableComponent? draggable)
    {
        if (!TryComp(target, out draggable))
            return false;

        // CanSeeTable checks interaction action blockers. So no need to check them here.
        // If this ever changes, so that ghosts can spectate games, then the check needs to be moved here.

        return TryComp<HandsComponent>(playerEntity, out var hands) && hands.Hands.Count > 0;
    }

    #endregion
}
