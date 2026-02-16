using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
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
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

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
            foreach (var target in stealTargets)
            {
                if ((EntityUid)target == uid)
                {
                    continue;
                }

                var targetTransform = Transform(target);
                if (inventoryVacuum.ShouldCheckLineOfSight
                    && !_interactionSystem.InRangeAndAccessible((uid, transform),
                        (target, targetTransform),
                        inventoryVacuum.StealRange))
                {
                    continue;
                }

                var stolenItem = TrySteal((uid, inventoryVacuum), target);
                if (stolenItem is not null)
                {
                    inventoryVacuum.NextStealAttempt = now + inventoryVacuum.StealAttemptCooldown;
                    _adminLogger.Add(LogType.Action,
                        LogImpact.Medium,
                        $"{ToPrettyString(uid):actor} stole item {ToPrettyString(stolenItem):item} from {ToPrettyString(target):subject} due to having inventory vacuum");
                    break;
                }
            }
        }
    }

    private EntityUid? TrySteal(
        Entity<InventoryVacuumComponent> ent,
        Entity<InventoryComponent> target)
    {
        var targetInventory = _inventorySystem.GetHandOrInventoryEntities(target.Owner);
        foreach (var targetInventoryItem in targetInventory)
        {
            _inventorySystem.TryGetContainingSlot(targetInventoryItem, out var slot);
            // Steal from the inventory steal whitelist or hands.
            if (slot is null
                || ent.Comp.StealSlotWhitelist.Contains(slot.Name)
                || ent.Comp.StealSlotWhitelist.Count == 0)
            {
                if (_inventorySystem.TryGetSlotEntity(ent, "back", out var uidBackpack))
                {
                    var containers = _containerSystem.GetAllContainers(uidBackpack.Value);
                    if (containers.Any() && _containerSystem.Insert(targetInventoryItem, containers.First()))
                    {
                        return targetInventoryItem;
                    }
                }

                if (_handsSystem.TryPickupAnyHand(ent, targetInventoryItem))
                {
                    return targetInventoryItem;
                }
            }
        }

        return null;
    }
}
