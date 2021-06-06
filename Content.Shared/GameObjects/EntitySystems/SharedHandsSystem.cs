using Content.Shared.GameObjects.Components.Items;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using System;

namespace Content.Shared.GameObjects.EntitySystems
{
    public abstract class SharedHandsSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedHandsComponent, EntRemovedFromContainerMessage>(HandleContainerModified);
            SubscribeLocalEvent<SharedHandsComponent, EntInsertedIntoContainerMessage>(HandleContainerModified);

            SubscribeLocalEvent<RequestSwapHandsevent>(HandleSwapHands);
            SubscribeNetworkEvent<RequestSwapHandsevent>(HandleSwapHands);

            SubscribeLocalEvent<RequestDropHeldEntityEvent>(HandleDrop);
            SubscribeNetworkEvent<RequestDropHeldEntityEvent>(HandleDrop);
        }

        private void HandleSwapHands(RequestSwapHandsevent msg, EntitySessionEventArgs eventArgs)
        {
            var entity = eventArgs.SenderSession?.AttachedEntity;

            if (entity == null || !entity.TryGetComponent(out SharedHandsComponent? hands))
                return;

            hands.SwapHands();
        }

        private void HandleDrop(RequestDropHeldEntityEvent msg, EntitySessionEventArgs eventArgs)
        {
            var entity = eventArgs.SenderSession?.AttachedEntity;

            if (entity == null || !entity.TryGetComponent(out SharedHandsComponent? hands))
                return;

            hands.TryDropActiveHand(msg.DropTarget);
        }

        protected abstract void HandleContainerModified(EntityUid uid, SharedHandsComponent component, ContainerModifiedMessage args);
    }

    [Serializable, NetSerializable]
    public class RequestSwapHandsevent : EntityEventArgs
    {
    }

    [Serializable, NetSerializable]
    public class RequestDropHeldEntityEvent : EntityEventArgs
    {
        public EntityCoordinates DropTarget { get; }

        public RequestDropHeldEntityEvent(EntityCoordinates dropTarget)
        {
            DropTarget = dropTarget;
        }
    }
}
