using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.Hands.Systems
{
    [UsedImplicitly]
    public sealed class HandVirtualItemSystem : SharedHandVirtualItemSystem
    {
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

        public bool TrySpawnVirtualItemInHand(EntityUid blockingEnt, EntityUid user) => TrySpawnVirtualItemInHand(blockingEnt, user, out _);

        public bool TrySpawnVirtualItemInHand(EntityUid blockingEnt, EntityUid user, [NotNullWhen(true)] out EntityUid? virtualItem)
        {
            if (!_handsSystem.TryGetEmptyHand(user, out var hand))
            {
                virtualItem = null;
                return false;
            }

            var pos = EntityManager.GetComponent<TransformComponent>(user).Coordinates;
            virtualItem = EntityManager.SpawnEntity("HandVirtualItem", pos);
            var virtualItemComp = EntityManager.GetComponent<HandVirtualItemComponent>(virtualItem.Value);
            virtualItemComp.BlockingEntity = blockingEnt;
            _handsSystem.DoPickup(user, hand, virtualItem.Value);
            return true;
        }

        /// <summary>
        ///     Deletes all virtual items in a user's hands with
        ///     the specified blocked entity.
        /// </summary>
        public void DeleteInHandsMatching(EntityUid user, EntityUid matching)
        {
            foreach (var hand in _handsSystem.EnumerateHands(user))
            {
                if (TryComp(hand.HeldEntity, out HandVirtualItemComponent? virt) && virt.BlockingEntity == matching)
                {
                    Delete(virt, user);
                }
            }
        }
    }
}
