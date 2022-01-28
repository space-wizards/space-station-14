using Content.Server.Hands.Components;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.Hands.Systems
{
    [UsedImplicitly]
    public sealed class HandVirtualItemSystem : SharedHandVirtualItemSystem
    {
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

        public bool TrySpawnVirtualItemInHand(EntityUid blockingEnt, EntityUid user)
        {
            if (EntityManager.TryGetComponent<HandsComponent>(user, out var hands))
            {
                foreach (var handName in hands.ActivePriorityEnumerable())
                {
                    var hand = hands.GetHand(handName);
                    if (hand.HeldEntity != null)
                        continue;

                    var pos = EntityManager.GetComponent<TransformComponent>(hands.Owner).Coordinates;
                    var virtualItem = EntityManager.SpawnEntity("HandVirtualItem", pos);
                    var virtualItemComp = EntityManager.GetComponent<HandVirtualItemComponent>(virtualItem);
                    virtualItemComp.BlockingEntity = blockingEnt;
                    _handsSystem.PutEntityIntoHand(user, hand, virtualItem, hands);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Deletes all virtual items in a user's hands with
        ///     the specified blocked entity.
        /// </summary>
        public void DeleteInHandsMatching(EntityUid user, EntityUid matching)
        {
            if (!EntityManager.TryGetComponent<HandsComponent>(user, out var hands))
                return;

            foreach (var handName in hands.ActivePriorityEnumerable())
            {
                var hand = hands.GetHand(handName);

                if (!(hand.HeldEntity is { } heldEntity))
                    continue;

                if (EntityManager.TryGetComponent<HandVirtualItemComponent>(heldEntity, out var virt)
                    && virt.BlockingEntity == matching)
                {
                    Delete(virt, user);
                }
            }
        }
    }
}
