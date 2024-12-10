using System.Diagnostics.CodeAnalysis;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Inventory;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;

namespace Content.Server.Objectives.Systems;

/// <summary>
///     The system for dealing with spawning the items for the GiveItemsForObjectiveComponent.
/// </summary>
public sealed class GiveItemsForObjectiveSystem : EntitySystem
{
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GiveItemsForObjectiveComponent, ObjectiveAddedToMindEvent>(OnAddedToMind, before: new[] { typeof(StealConditionSystem) });
        SubscribeLocalEvent<GiveItemsForObjectiveComponent, ObjectiveAssignedEvent>(OnAssign);
    }

    private void OnAssign(Entity<GiveItemsForObjectiveComponent> entity, ref ObjectiveAssignedEvent args)
    {
        // All this is to check if the item can actually fit in someones backpack.

        var mindOwner = args.Mind.OwnedEntity;
        if (mindOwner == null)
        {
            args.Cancelled = true;
            return;
        }

        if (!_prototypeManager.TryIndex<WeightedRandomPrototype>(entity.Comp.ItemsToSpawn, out var itemWeights))
        {
            Log.Error($"Item weights are not a valid prototype: {entity.Comp.ItemsToSpawn}");
            return;
        }

        // It doesn't matter if the item can fit or not so just skip checking.
        if (entity.Comp.CancelAssignmentOnNoSpace == false)
            return;

        // Check if all the items can fit! If there is an item that can't, cancel the assignment.
        foreach (var itemAndWeight in itemWeights.Weights)
        {
            var obj = Spawn(itemAndWeight.Key);
            var canFit = _inventorySystem.CanItemFitOnEntity(mindOwner.Value, obj);
            Del(obj);

            if (!canFit)
            {
                args.Cancelled = true;
                return;
            }

        }

    }

    private void OnAddedToMind(Entity<GiveItemsForObjectiveComponent> entity, ref ObjectiveAddedToMindEvent args)
    {
        // At this point we are *always* going to try to spawn the item. If the player can't hold the item then
        // will just be dropped on the ground.

        if (!_prototypeManager.TryIndex<WeightedRandomPrototype>(entity.Comp.ItemsToSpawn, out var itemWeights))
        {
            Log.Error($"Item weights are not a valid prototype: {entity.Comp.ItemsToSpawn}");
            return;
        }

        var mindOwner = args.Mind.OwnedEntity;

        if (mindOwner == null)
            throw new Exception($"Mind owner is null.");

        var cords = _transformSystem.GetMapCoordinates(mindOwner.Value);

        var itemWeightsCopy = itemWeights.Weights.ShallowClone();
        while (_random.TryPickAndTake(itemWeightsCopy, out var chosenItem))
        {
            var obj = Spawn(chosenItem, cords);

            var beforeEvnt = new BeforeObjectiveItemGivenEvent(obj, false);
            RaiseLocalEvent(entity, ref beforeEvnt);

            // The item is good to spawn and we can stop if you get in this statment.
            if (!beforeEvnt.Retry)
            {
                var evnt = new ObjectiveItemGivenEvent(obj);
                RaiseLocalEvent(entity, ref evnt);

                // If their inventory is full, it will be spawned on the ground
                _inventorySystem.GiveItemToEntity(mindOwner.Value, obj);

                break;
            }

            Del(obj);
        }

    }

}
