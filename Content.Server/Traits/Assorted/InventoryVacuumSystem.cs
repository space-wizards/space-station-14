using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Traits.Assorted;

public sealed class InventoryVacuumSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    private const string StolenItemHideContainerSlot = "back";

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var timeNow = _gameTiming.CurTime;

        var inventoryVacuumQuery = EntityQueryEnumerator<InventoryVacuumComponent>();
        while (inventoryVacuumQuery.MoveNext(out var uid, out var inventoryVacuum))
        {
            if (timeNow < inventoryVacuum.NextStealAttempt)
            {
                continue;
            }

            inventoryVacuum.NextStealAttempt = timeNow + inventoryVacuum.StealAttemptCooldown;

            if (!_random.Prob(inventoryVacuum.StealChance))
            {
                continue;
            }

            var uidTransform = Transform(uid);
            var stealTargets =
                _lookupSystem.GetEntitiesInRange<InventoryComponent>(uidTransform.Coordinates, inventoryVacuum.StealRange);
            foreach (var target in stealTargets)
            {
                if (target.Owner == uid)
                {
                    continue;
                }

                var targetTransform = Transform(target);
                if (inventoryVacuum.ShouldCheckLineOfSight
                    && !_interactionSystem.InRangeAndAccessible((uid, uidTransform),
                        (target, targetTransform),
                        inventoryVacuum.StealRange))
                {
                    continue;
                }

                var stolenItem = TrySteal((uid, inventoryVacuum), target);
                if (stolenItem is not null)
                {
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
        var targetInventory = _inventorySystem.GetHandOrInventoryEntities(target.Owner).ToArray();
        _random.Shuffle(targetInventory);

        foreach (var targetInventoryItem in targetInventory)
        {
            _inventorySystem.TryGetContainingSlot(targetInventoryItem, out var slot);
            // Steal from the inventory steal whitelist or from hands, into backpack or our hand.
            if (slot is null
                || ent.Comp.StealSlotWhitelist.Contains(slot.Name)
                || ent.Comp.StealSlotWhitelist.Count == 0)
            {
                if (_inventorySystem.TryGetSlotEntity(ent, StolenItemHideContainerSlot, out var hideItemInto))
                {
                    var containerHideInto = _containerSystem.GetAllContainers(hideItemInto.Value);
                    if (containerHideInto.Any() && _containerSystem.Insert(targetInventoryItem, containerHideInto.First()))
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
