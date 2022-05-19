using Content.Server.Tabletop.Components;
using Content.Shared.Tabletop;
using Content.Shared.Tabletop.Components;
using Content.Shared.Tabletop.Events;
using Robust.Server.Player;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Server.Tabletop
{
    public sealed partial class TabletopSystem
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
            if (args.SenderSession as IPlayerSession is not { AttachedEntity: { } playerEntity } playerSession)
                return;

            if (!EntityManager.TryGetComponent(msg.TableUid, out TabletopGameComponent? tabletop) || tabletop.Session is not {} session)
                return;

            // Check if player is actually playing at this table
            if (!session.Players.ContainsKey(playerSession))
                return;

            if (!CanSeeTable(playerEntity, msg.TableUid) || !CanDrag(playerEntity, msg.MovedEntityUid, out _))
                return;

            // TODO: some permission system, disallow movement if you're not permitted to move the item

            // Move the entity and dirty it (we use the map ID from the entity so noone can try to be funny and move the item to another map)
            var transform = EntityManager.GetComponent<TransformComponent>(msg.MovedEntityUid);
            var entityCoordinates = new EntityCoordinates(_mapManager.GetMapEntityId(transform.MapID), msg.Coordinates.Position);
            transform.Coordinates = entityCoordinates;
        }

        private void OnDraggingPlayerChanged(TabletopDraggingPlayerChangedEvent msg, EntitySessionEventArgs args)
        {
            var dragged = msg.DraggedEntityUid;

            if (!EntityManager.TryGetComponent<TabletopDraggableComponent?>(dragged, out var draggableComponent)) return;

            draggableComponent.DraggingPlayer = msg.IsDragging ? args.SenderSession.UserId : null;
            Dirty(draggableComponent);

            if (!EntityManager.TryGetComponent<AppearanceComponent?>(dragged, out var appearance)) return;

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
