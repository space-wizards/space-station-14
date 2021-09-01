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
            if (ComponentManager.TryGetComponent<HandsComponent>(user, out var hands))
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
            Delete(component, args.User);
        }

        private void HandleItemDropped(EntityUid uid, HandVirtualItemComponent component, DroppedEvent args)
        {
            Delete(component, args.User);
        }

        private void Delete(HandVirtualItemComponent comp, IEntity user)
        {
            var ev = new VirtualItemDeletedEvent(comp.BlockingEntity, user.Uid);
            RaiseLocalEvent(user.Uid, ev, false);

            comp.Owner.QueueDelete();
        }
    }
}
