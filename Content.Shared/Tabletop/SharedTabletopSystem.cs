using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Tabletop.Components;
using Content.Shared.Tabletop.Events;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.Tabletop
{
    public abstract class SharedTabletopSystem : EntitySystem
    {
        [Dependency] protected readonly ActionBlockerSystem ActionBlockerSystem = default!;
        [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] protected readonly SharedTransformSystem Transforms = default!;
        [Dependency] private readonly IMapManager _mapMan = default!;

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
            if (args.SenderSession is not { AttachedEntity: { } playerEntity } playerSession)
                return;

            var table = GetEntity(msg.TableUid);
            var moved = GetEntity(msg.MovedEntityUid);

            if (!CanSeeTable(playerEntity, table) || !CanDrag(playerEntity, moved, out _))
                return;

            // Move the entity and dirty it (we use the map ID from the entity so noone can try to be funny and move the item to another map)
            var transform = EntityManager.GetComponent<TransformComponent>(moved);
            Transforms.SetParent(moved, transform, _mapMan.GetMapEntityId(transform.MapID));
            Transforms.SetLocalPositionNoLerp(transform, msg.Coordinates.Position);
        }

        private void OnDraggingPlayerChanged(TabletopDraggingPlayerChangedEvent msg, EntitySessionEventArgs args)
        {
            var dragged = GetEntity(msg.DraggedEntityUid);

            if (!TryComp(dragged, out TabletopDraggableComponent? draggableComponent))
                return;

            draggableComponent.DraggingPlayer = msg.IsDragging ? args.SenderSession.UserId : null;
            Dirty(dragged, draggableComponent);

            if (!TryComp(dragged, out AppearanceComponent? appearance))
                return;

            if (draggableComponent.DraggingPlayer != null)
            {
                _appearance.SetData(dragged, TabletopItemVisuals.Scale, new Vector2(1.25f, 1.25f), appearance);
                _appearance.SetData(dragged, TabletopItemVisuals.DrawDepth, (int) DrawDepth.DrawDepth.Items + 1, appearance);
            }
            else
            {
                _appearance.SetData(dragged, TabletopItemVisuals.Scale, Vector2.One, appearance);
                _appearance.SetData(dragged, TabletopItemVisuals.DrawDepth, (int) DrawDepth.DrawDepth.Items, appearance);
            }
        }


        [Serializable, NetSerializable]
        public sealed class TabletopDraggableComponentState : ComponentState
        {
            public NetUserId? DraggingPlayer;

            public TabletopDraggableComponentState(NetUserId? draggingPlayer)
            {
                DraggingPlayer = draggingPlayer;
            }
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

            return _interactionSystem.InRangeUnobstructed(playerEntity, table.Value) && ActionBlockerSystem.CanInteract(playerEntity, table);
        }

        protected bool CanDrag(EntityUid playerEntity, EntityUid target, [NotNullWhen(true)] out TabletopDraggableComponent? draggable)
        {
            if (!TryComp(target, out draggable))
                return false;

            // CanSeeTable checks interaction action blockers. So no need to check them here.
            // If this ever changes, so that ghosts can spectate games, then the check needs to be moved here.

            return TryComp(playerEntity, out HandsComponent? hands) && hands.Hands.Count > 0;
        }
        #endregion
    }
}
