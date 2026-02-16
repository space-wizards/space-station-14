using System.Linq;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Random.Helpers;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Traits.Assorted;

public sealed class InventoryVacuumSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _gameTiming.CurTime;

        var query = EntityQueryEnumerator<InventoryVacuumComponent>();
        while (query.MoveNext(out var uid, out var inventoryVacuum))
        {
            if (now < inventoryVacuum.NextStealAttempt)
            {
                continue;
            }

            // TODO: Replace with RandomPredicted once the engine PR is merged (#5849)
            var seed = SharedRandomExtensions.HashCodeCombine((int)_gameTiming.CurTick.Value, GetNetEntity(uid).Id);
            var rand = new System.Random(seed);
            if (rand.NextFloat() > inventoryVacuum.StealChance)
            {
                inventoryVacuum.NextStealAttempt = now + inventoryVacuum.StealAttemptCooldown;
                continue;
            }

            var transform = Transform(uid);
            var stealTargets =
                _lookupSystem.GetEntitiesInRange<InventoryComponent>(transform.Coordinates, inventoryVacuum.StealRange);
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

                var targetInventory = _inventorySystem.GetHandOrInventoryEntities(target.Owner);
                foreach (var targetInventoryItem in targetInventory)
                {
                    _inventorySystem.TryGetContainingSlot(targetInventoryItem, out var slot);
                    if (slot is null
                        || inventoryVacuum.StealSlotWhitelist.Contains(slot.Name)
                        || inventoryVacuum.StealSlotWhitelist.Count == 0)
                    {
                        if (
                            (
                                _inventorySystem.TryGetSlotEntity(uid, "back", out var uidBackpack)
                                && _containerSystem.Insert(targetInventoryItem,
                                    _containerSystem.GetAllContainers(uidBackpack.Value).First())
                            )
                            || _handsSystem.TryPickupAnyHand(uid, targetInventoryItem))
                        {
                            inventoryVacuum.NextStealAttempt = now + inventoryVacuum.StealAttemptCooldown;
                            stolenItem = targetInventoryItem;
                            break;
                        }
                    }
                }
            }
        }
    }
}
