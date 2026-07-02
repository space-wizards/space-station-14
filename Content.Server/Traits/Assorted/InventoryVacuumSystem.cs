using System.Linq;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
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

    /// <summary>
    /// Where we try to insert the stolen item.
    /// </summary>
    private const string StolenItemHideContainerSlot = "back";

    /// <summary>
    /// Whitelist of slots where the item is a container, and we want to grab from inside the item
    /// instead of grabbing the item itself. This makes us steal from inside backpacks and belts,
    /// but not radio keys from headsets, paper from paper trays, etc.
    /// </summary>
    private static readonly HashSet<string> TargetItemContainerSlotWhitelist = ["back", "belt"];


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

            inventoryVacuum.NextStealAttempt += inventoryVacuum.StealAttemptCooldown;

            if (!_random.Prob(inventoryVacuum.StealChance))
            {
                continue;
            }

            var uidTransform = Transform(uid);
            var stealTargets =
                _lookupSystem.GetEntitiesInRange<InventoryComponent>(uidTransform.Coordinates,
                    inventoryVacuum.StealRange);
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

        EntityUid? targetItem;
        foreach (var targetItemCandidate in targetInventory)
        {
            _inventorySystem.TryGetContainingSlot(targetItemCandidate, out var slot);

            var targetItemContainers = _containerSystem.GetAllContainers(targetItemCandidate);
            if (slot is not null
                && TargetItemContainerSlotWhitelist.Contains(slot.Name)
                && targetItemContainers.Any())
            {
                var candidateContainer = targetItemContainers.First();
                targetItem = _random.Pick(candidateContainer.ContainedEntities);
            }
            else
            {
                targetItem = targetItemCandidate;
            }

            // Steal from the inventory steal whitelist or from hands, into backpack or our hand.
            if (_handsSystem.IsHolding(target.Owner, targetItemCandidate)
                || (slot is not null && ent.Comp.StealSlotWhitelist.Contains(slot.Name))
                || ent.Comp.StealSlotWhitelist.Count == 0)
            {
                if (_inventorySystem.TryGetSlotEntity(ent, StolenItemHideContainerSlot, out var hideItemInto))
                {
                    var containerHideInto = _containerSystem.GetAllContainers(hideItemInto.Value);
                    if (containerHideInto.Any()
                        && _handsSystem.CanPickupAnyHand(ent, targetItem.Value)
                        && _containerSystem.Insert(targetItem.Value, containerHideInto.First()))
                    {
                        return targetItem;
                    }
                }

                if (_handsSystem.TryPickupAnyHand(ent, targetItem.Value))
                {
                    return targetItem;
                }
            }
        }

        return null;
    }
}
