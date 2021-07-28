using Content.Shared.Hands.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
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

            SubscribeAllEvent<RequestSetHandEvent>(HandleSetHand);
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

            if (msg.Cancelled)
                return;

            if (entity.TryGetContainerMan(out var containerManager))
            {
                var parentMsg = new ContainedEntityDropHandItemsAttemptEvent(uid);
                eventBus.RaiseLocalEvent(containerManager.Owner.Uid, parentMsg);

                if (parentMsg.Cancelled)
                    return;
            }

            DropAllItemsInHands(entity, doMobChecks);
        }

        protected virtual void DropAllItemsInHands(IEntity entity, bool doMobChecks = true)
        {
        }

        private static void HandleSetHand(RequestSetHandEvent msg, EntitySessionEventArgs eventArgs)
        {
            var entity = eventArgs.SenderSession.AttachedEntity;

            if (entity == null || !entity.TryGetComponent(out SharedHandsComponent? hands))
                return;

            hands.ActiveHand = msg.HandName;
        }

        protected virtual void HandleContainerModified(
            EntityUid uid,
            SharedHandsComponent component,
            ContainerModifiedMessage args)
        {
            component.Dirty();
        }
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
}
