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

        public void DropHandItems(IEntity entity, bool doMobChecks = true)
        {
            if (!entity.TryGetComponent(out SharedHandsComponent? handsComponent)) return;
            DropHandItems(handsComponent, doMobChecks);
        }

        private void DropHandItems(SharedHandsComponent handsComponent, bool doMobChecks = true)
        {
            var msg = new DropHandItemsAttemptEvent();
            var entity = handsComponent.Owner;
            var uid = entity.Uid;
            var eventBus = EntityManager.EventBus;

            eventBus.RaiseLocalEvent(uid, msg);

            if (msg.Cancelled) return;

            if (entity.TryGetContainerMan(out var containerManager))
            {
                var parentMsg = new ContainedEntityDropHandItemsAttemptEvent(uid);
                eventBus.RaiseLocalEvent(containerManager.Owner.Uid, parentMsg);

                if (parentMsg.Cancelled) return;
            }

            DropAllItemsInHands(entity, doMobChecks);
        }

        protected virtual void DropAllItemsInHands(IEntity entity, bool doMobChecks = true)
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

    public sealed class ContainedEntityDropHandItemsAttemptEvent : CancellableEntityEventArgs
    {
        public EntityUid EntityUid { get; }

        public ContainedEntityDropHandItemsAttemptEvent(EntityUid uid)
        {
            EntityUid = uid;
        }
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
