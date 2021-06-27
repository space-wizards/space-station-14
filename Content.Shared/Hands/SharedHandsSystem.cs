using Content.Shared.Hands.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using System;

namespace Content.Shared.Hands
{
    public abstract class SharedHandsSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedHandsComponent, EntRemovedFromContainerMessage>(HandleContainerModified);
            SubscribeLocalEvent<SharedHandsComponent, EntInsertedIntoContainerMessage>(HandleContainerModified);

            SubscribeLocalEvent<RequestSetHandEvent>(HandleSetHand);
            SubscribeNetworkEvent<RequestSetHandEvent>(HandleSetHand);

            SubscribeLocalEvent<RequestDropHeldEntityEvent>(HandleDrop);
            SubscribeNetworkEvent<RequestDropHeldEntityEvent>(HandleDrop);
        }

        public void DropHandItems(IEntity entity)
        {
            if (!entity.TryGetComponent(out SharedHandsComponent? handsComponent)) return;
            DropHandItems(handsComponent);
        }

        private void DropHandItems(SharedHandsComponent handsComponent)
        {
            var msg = new DropHandItemsAttemptEvent();
            EntityManager.EventBus.RaiseLocalEvent(handsComponent.Owner.Uid, msg);

            if (msg.Cancelled) return;

            DropAllItemsInHands(handsComponent.Owner, false);
        }

        public virtual void DropAllItemsInHands(IEntity entity, bool doMobChecks = true)
        {
        }

        private void HandleSetHand(RequestSetHandEvent msg, EntitySessionEventArgs eventArgs)
        {
            var entity = eventArgs.SenderSession?.AttachedEntity;

            if (entity == null || !entity.TryGetComponent(out SharedHandsComponent? hands))
                return;

            hands.ActiveHand = msg.HandName;
        }

        private void HandleDrop(RequestDropHeldEntityEvent msg, EntitySessionEventArgs eventArgs)
        {
            var entity = eventArgs.SenderSession?.AttachedEntity;

            if (entity == null || !entity.TryGetComponent(out SharedHandsComponent? hands))
                return;

            hands.TryDropHand(msg.HandName, msg.DropTarget);
        }

        protected abstract void HandleContainerModified(EntityUid uid, SharedHandsComponent component, ContainerModifiedMessage args);
    }

    public sealed class DropHandItemsAttemptEvent : CancellableEntityEventArgs {}

    [Serializable, NetSerializable]
    public class RequestSetHandEvent : EntityEventArgs
    {
        /// <summary>
        ///     The hand to be swapped to.
        /// </summary>
        public string HandName { get; }

        public RequestSetHandEvent(string handName)
        {
            HandName = handName;
        }
    }

    [Serializable, NetSerializable]
    public class RequestDropHeldEntityEvent : EntityEventArgs
    {
        /// <summary>
        ///     The hand to drop from.
        /// </summary>
        public string HandName { get; }

        public EntityCoordinates DropTarget { get; }

        public RequestDropHeldEntityEvent(string handName, EntityCoordinates dropTarget)
        {
            HandName = handName;
            DropTarget = dropTarget;
        }
    }
}
