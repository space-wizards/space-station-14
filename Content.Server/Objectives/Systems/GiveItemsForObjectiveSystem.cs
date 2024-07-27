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

        if (!GetBackpackStorageAndSlot(mindOwner.Value, out var slot, out var storageComp))
        {
            args.Cancelled |= entity.Comp.CancelAssignmentOnNoSpace;
            return;
        }

        // Check if all the items can fit!
        foreach (var item in entity.Comp.ItemsToSpawnPrototypes)
        {
            var obj = Spawn(item);

            if (!_storage.CanInsert(slot.Value, obj, out _, storageComp: storageComp))
                args.Cancelled |= entity.Comp.CancelAssignmentOnNoSpace;

            Del(obj);
        }

    }

    private void OnAddedToMind(Entity<GiveItemsForObjectiveComponent> entity, ref ObjectiveAddedToMindEvent args)
    {
        // At this point we are always going to try to spawn the item. If the players backpack is full then the item
        // will just be dropped on the ground.

        var mindOwner = args.Mind.OwnedEntity;

        if (mindOwner == null)
            throw new Exception($"Mind owner is null.");

        // Spawn the item at the players location.
        var cords = _transformSystem.GetMapCoordinates(mindOwner.Value);
        EntityUid obj = Spawn(_random.Pick(entity.Comp.ItemsToSpawnPrototypes), cords);

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

            if (i == attempts - 1)
                throw new Exception($"Could not spawn a valid entity within {attempts}.");
        }

        var evnt = new ObjectiveItemGivenEvent(obj);
        RaiseLocalEvent(entity, ref evnt);

        // If this fails, thats OK. The item was already spawned on the ground.
        if (!GetBackpackStorageAndSlot(mindOwner.Value, out var slot, out var storageComp))
            return;

        _storage.Insert(slot.Value, obj, out _, storageComp: storageComp, playSound: false);
    }

    /// <summary>
    ///     If the backpack slot doesn't exist, is empty, or the is full (With a non storage item) will return null.
    ///     Otherwise will return the storage slot and the storage itself.
    /// </summary>
    private bool GetBackpackStorageAndSlot(EntityUid uid, [NotNullWhen(true)] out EntityUid? slot, [NotNullWhen(true)] out StorageComponent? storage)
    {
        slot = null;
        storage = null;

        if (!TryComp<InventoryComponent>(uid, out var inventoryComp))
            return false;

        if (!(_inventorySystem.TryGetSlotEntity(uid, "back", out var slotEnt, inventoryComponent: inventoryComp) &&
            TryComp<StorageComponent>(slotEnt, out var storageComp)))
            return false;

        if (slotEnt == null || storageComp == null)
            return false;

        slot = slotEnt.Value;
        storage = storageComp;
        return true;
    }

}
