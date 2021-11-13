using Content.Server.Tabletop.Components;
using Content.Shared.Tabletop;
using Content.Shared.Tabletop.Events;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Server.Tabletop
{
    public partial class TabletopSystem
    {
        public void InitializeDraggable()
        {
            SubscribeNetworkEvent<TabletopMoveEvent>(OnTabletopMove);
            SubscribeNetworkEvent<TabletopDraggingPlayerChangedEvent>(OnDraggingPlayerChanged);
            SubscribeLocalEvent<TabletopDraggableComponent, ComponentGetState>(GetDraggableState);
        }

        /// <summary>
        ///     Move an entity which is dragged by the user, but check if they are allowed to do so and to these coordinates
        /// </summary>
        private void OnTabletopMove(TabletopMoveEvent msg, EntitySessionEventArgs args)
        {
            if (args.SenderSession as IPlayerSession is not { AttachedEntityUid: { } playerEntity } playerSession)
                return;

            if (!EntityManager.TryGetComponent(msg.TableUid, out TabletopGameComponent? tabletop) || tabletop.Session is not {} session)
                return;

            // Check if player is actually playing at this table
            if (!session.Players.ContainsKey(playerSession))
                return;

            // Return if can not see table or stunned/no hands
            if (!EntityManager.EntityExists(msg.TableUid))
                return;

            if (!CanSeeTable(playerEntity, msg.TableUid) || StunnedOrNoHands(playerEntity))
                return;

            // Check if moved entity exists and has tabletop draggable component
            if (!EntityManager.TryGetEntity(msg.MovedEntityUid, out var movedEntity))
                return;

            if (!EntityManager.HasComponent<TabletopDraggableComponent>(movedEntity.Uid))
                return;

            // TODO: some permission system, disallow movement if you're not permitted to move the item

            // Move the entity and dirty it (we use the map ID from the entity so noone can try to be funny and move the item to another map)
            var transform = EntityManager.GetComponent<TransformComponent>(movedEntity.Uid);
            var entityCoordinates = new EntityCoordinates(_mapManager.GetMapEntityId(transform.MapID), msg.Coordinates.Position);
            transform.Coordinates = entityCoordinates;
        }

        private void OnDraggingPlayerChanged(TabletopDraggingPlayerChangedEvent msg)
        {
            var draggedEntity = EntityManager.GetEntity(msg.DraggedEntityUid);

            if (!draggedEntity.TryGetComponent<TabletopDraggableComponent>(out var draggableComponent)) return;

            draggableComponent.DraggingPlayer = msg.DraggingPlayer;

            if (!draggedEntity.TryGetComponent<AppearanceComponent>(out var appearance)) return;

            if (draggableComponent.DraggingPlayer != null)
            {
                appearance.SetData(TabletopItemVisuals.Scale, new Vector2(1.25f, 1.25f));
                appearance.SetData(TabletopItemVisuals.DrawDepth, (int) DrawDepth.Items + 1);
            }
            else
            {
                appearance.SetData(TabletopItemVisuals.Scale, Vector2.One);
                appearance.SetData(TabletopItemVisuals.DrawDepth, (int) DrawDepth.Items);
            }
        }

        private void GetDraggableState(EntityUid uid, TabletopDraggableComponent component, ref ComponentGetState args)
        {
            args.State = new TabletopDraggableComponentState(component.DraggingPlayer);
        }
    }
}
