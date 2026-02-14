using System.Linq;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Robust.Server.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Traits.Assorted;

public sealed class KleptomaniacSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;

    public const float StealRange = SharedInteractionSystem.InteractionRange;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        var query = EntityQueryEnumerator<KleptomaniacComponent>();
        while (query.MoveNext(out var uid, out var klepto))
        {
            if (now < klepto.NextStealAttempt)
            {
                continue;
            }

            if (_random.NextFloat() > klepto.StealChance)
            {
                klepto.NextStealAttempt = now + klepto.StealAttemptCooldown;
                continue;
            }

            var transform = Transform(uid);
            var stealTargets = _lookupSystem.GetEntitiesInRange<InventoryComponent>(transform.Coordinates, StealRange);
            EntityUid? stolenItem = null;
            foreach (var target in stealTargets)
            {
                if (stolenItem is not null)
                {
                    break;
                }

                if ((EntityUid)target == uid)
                {
                    continue;
                }

                var targetTransform = Transform(target);
                if (!_interactionSystem.InRangeUnobstructed((uid, transform), (target, targetTransform), StealRange))
                {
                    continue;
                }

                var targetInventory = _inventorySystem.GetHandOrInventoryEntities(target.Owner);
                foreach (var targetInventoryItem in targetInventory)
                {
                    _inventorySystem.TryGetContainingSlot(targetInventoryItem, out var slot);
                    if (slot is null || slot.Name is "hand" or "pocket1" or "pocket2")
                    {
                        if (
                            (
                                _inventorySystem.TryGetSlotEntity(uid, "back", out var uidBackpack)
                                && _containerSystem.Insert(targetInventoryItem,
                                    _containerSystem.GetAllContainers(uidBackpack.Value).First())
                            )
                            || _handsSystem.TryPickupAnyHand(uid, targetInventoryItem))
                        {
                            klepto.NextStealAttempt = now + klepto.StealAttemptCooldown;
                            stolenItem = targetInventoryItem;
                            break;
                        }
                    }
                }
            }
        }
    }
}
