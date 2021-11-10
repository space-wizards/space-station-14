using Content.Server.Hands.Components;
using Content.Server.Pulling;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Hands.Systems
{
    [UsedImplicitly]
    public sealed class HandVirtualItemSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<HandVirtualItemComponent, DroppedEvent>(HandleItemDropped);
            SubscribeLocalEvent<HandVirtualItemComponent, UnequippedHandEvent>(HandleItemUnequipped);

            SubscribeLocalEvent<HandVirtualItemComponent, BeforeInteractEvent>(HandleBeforeInteract);
        }

        public bool TrySpawnVirtualItemInHand(EntityUid blockingEnt, EntityUid user)
        {
            if (EntityManager.TryGetComponent<HandsComponent>(user, out var hands))
            {
                foreach (var handName in hands.ActivePriorityEnumerable())
                {
                    var hand = hands.GetHand(handName);
                    if (!hand.IsEmpty)
                        continue;

                    var pos = hands.Owner.Transform.Coordinates;
                    var virtualItem = EntityManager.SpawnEntity("HandVirtualItem", pos);
                    var virtualItemComp = virtualItem.GetComponent<HandVirtualItemComponent>();
                    virtualItemComp.BlockingEntity = blockingEnt;
                    hands.PutEntityIntoHand(hand, virtualItem);
                    return true;
                }
            }

            return false;
        }

        private static void HandleBeforeInteract(
            EntityUid uid,
            HandVirtualItemComponent component,
            BeforeInteractEvent args)
        {
            // No interactions with a virtual item, please.
            args.Handled = true;
        }

        // If the virtual item gets removed from the hands for any reason, cancel the pull and delete it.
        private void HandleItemUnequipped(EntityUid uid, HandVirtualItemComponent component, UnequippedHandEvent args)
        {
            Delete(component, args.User.Uid);
        }

        private void HandleItemDropped(EntityUid uid, HandVirtualItemComponent component, DroppedEvent args)
        {
            Delete(component, args.UserUid);
        }

        /// <summary>
        ///     Queues a deletion for a virtual item and notifies the blocking entity and user.
        /// </summary>
        public void Delete(HandVirtualItemComponent comp, EntityUid user)
        {
            var userEv = new VirtualItemDeletedEvent(comp.BlockingEntity, user);
            RaiseLocalEvent(user, userEv, false);
            var targEv = new VirtualItemDeletedEvent(comp.BlockingEntity, user);
            RaiseLocalEvent(comp.BlockingEntity, targEv, false);

            comp.Owner.QueueDelete();
        }

        /// <summary>
        ///     Deletes all virtual items in a user's hands with
        ///     the specified blocked entity.
        /// </summary>
        public void DeleteInHandsMatching(EntityUid user, EntityUid matching)
        {
            if (EntityManager.TryGetComponent<HandsComponent>(user, out var hands))
            {
                foreach (var handName in hands.ActivePriorityEnumerable())
                {
                    var hand = hands.GetHand(handName);
                    if (hand.IsEmpty)
                        continue;

                    if (hand.HeldEntity != null)
                    {
                        if (EntityManager.TryGetComponent<HandVirtualItemComponent>(hand.HeldEntity.Uid,
                                out var virt)
                            && virt.BlockingEntity == matching)
                        {
                            Delete(virt, user);
                        }
                    }
                }
            }
        }
    }
}
