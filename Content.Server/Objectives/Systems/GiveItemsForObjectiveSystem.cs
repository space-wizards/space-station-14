using System.Diagnostics.CodeAnalysis;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Content.Shared.Storage.EntitySystems;
using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Inventory;
using Content.Shared.Storage;

namespace Content.Server.Objectives.Systems;

/// <summary>
///     The system for dealing with spawning the items for the GiveItemsForObjectiveComponent.
/// </summary>
public sealed class GiveItemsForObjectiveSystem : EntitySystem
{
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

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

        // It doesn't matter if the item can fit or not so just skip checking.
        if (entity.Comp.CancelAssignmentOnNoSpace == false)
            return;

        // Check if all the items can fit!
        foreach (var item in entity.Comp.ItemsToSpawnPrototypes)
        {
            var obj = Spawn(item);

            if (!_inventorySystem.CanItemFitOnEntity(mindOwner.Value, obj))
            {
                Del(obj);
                args.Cancelled = true;
                return;
            }

            Del(obj);
        }

    }

    private void OnAddedToMind(Entity<GiveItemsForObjectiveComponent> entity, ref ObjectiveAddedToMindEvent args)
    {
        // At this point we are *always* going to try to spawn the item. If the player can't hold the item then
        // will just be dropped on the ground.

        var mindOwner = args.Mind.OwnedEntity;

        if (mindOwner == null)
            throw new Exception($"Mind owner is null.");

        // Spawn the item at the players location.
        var cords = _transformSystem.GetMapCoordinates(mindOwner.Value);
        var obj = Spawn(_random.Pick(entity.Comp.ItemsToSpawnPrototypes), cords);

        // The loop should never go more than a few times in very unlikely situations.
        var attempts = 30;
        for (var i = 0; i < attempts; i++)
        {
            var beforeEvnt = new BeforeObjectiveItemGivenEvent(obj, false);
            RaiseLocalEvent(entity, ref beforeEvnt);

            if (!beforeEvnt.Retry)
                break;

            Del(obj);
            obj = Spawn(_random.Pick(entity.Comp.ItemsToSpawnPrototypes), cords);

            // This is on the final iteration of the loop.
            if (i == attempts - 1)
                throw new Exception($"Could not spawn a valid entity within {attempts}.");
        }

        var evnt = new ObjectiveItemGivenEvent(obj);
        RaiseLocalEvent(entity, ref evnt);

        // If their inventory is full, it will be spawned on the ground
        _inventorySystem.GiveItemToEntity(mindOwner.Value, obj);
    }

}
