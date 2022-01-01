using System;
using Content.Shared.Hands.Components;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Content.Shared.Hands
{
    public abstract class SharedHandsSystem : EntitySystem
    {
        [Dependency] private readonly SharedHandVirtualItemSystem _virtualSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SharedHandsComponent, EntRemovedFromContainerMessage>(HandleContainerRemoved);
            SubscribeLocalEvent<SharedHandsComponent, EntInsertedIntoContainerMessage>(HandleContainerModified);

            SubscribeAllEvent<RequestSetHandEvent>(HandleSetHand);
        }

        private void HandleSetHand(RequestSetHandEvent msg, EntitySessionEventArgs eventArgs)
        {
            var entity = eventArgs.SenderSession.AttachedEntity;

            if (entity == null || !EntityManager.TryGetComponent(entity, out SharedHandsComponent? hands))
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

        private void HandleContainerRemoved(
            EntityUid uid,
            SharedHandsComponent component,
            ContainerModifiedMessage args)
            {
            if (TryComp(args.Entity, out HandVirtualItemComponent? @virtual))
                _virtualSystem.Delete(@virtual, uid);

            HandleContainerModified(uid, component, args);
        }
    }

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

    /// <summary>
    ///     Raised directed on both the blocking entity and user when
    ///     a virtual hand item is deleted.
    /// </summary>
    public class VirtualItemDeletedEvent : EntityEventArgs
    {
        public EntityUid BlockingEntity;
        public EntityUid User;

        public VirtualItemDeletedEvent(EntityUid blockingEntity, EntityUid user)
        {
            BlockingEntity = blockingEntity;
            User = user;
        }
    }
}
