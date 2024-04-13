using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Collections;

namespace Content.Shared.Station;

public abstract class SharedStationSpawningSystem : EntitySystem
{
    [Dependency] protected readonly InventorySystem InventorySystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

    /// <summary>
    /// Equips starting gear onto the given entity. Returns a list of all the spawned entities.
    /// </summary>
    /// <param name="entity">Entity to load out.</param>
    /// <param name="startingGear">Starting gear to use.</param>
    /// <param name="profile">Character profile to use, if any.</param>
    public List<EntityUid?> EquipStartingGear(EntityUid entity, StartingGearPrototype startingGear, HumanoidCharacterProfile? profile)
    {
        var spawnedItems = new List<EntityUid?>();

        if (InventorySystem.TryGetSlots(entity, out var slotDefinitions))
        {
            foreach (var slot in slotDefinitions)
            {
                var equipmentStr = startingGear.GetGear(slot.Name, profile);
                if (!string.IsNullOrEmpty(equipmentStr))
                {
                    var equipmentEntity = EntityManager.SpawnEntity(equipmentStr, EntityManager.GetComponent<TransformComponent>(entity).Coordinates);
                    InventorySystem.TryEquip(entity, equipmentEntity, slot.Name, true, force: true);
                    spawnedItems.Add(equipmentEntity);
                }
            }
        }

        if (!TryComp(entity, out HandsComponent? handsComponent))
            return spawnedItems;

        var inhand = startingGear.Inhand;
        var coords = EntityManager.GetComponent<TransformComponent>(entity).Coordinates;
        foreach (var prototype in inhand)
        {
            var inhandEntity = EntityManager.SpawnEntity(prototype, coords);

            if (_handsSystem.TryGetEmptyHand(entity, out var emptyHand, handsComponent))
            {
                _handsSystem.TryPickup(entity, inhandEntity, emptyHand, checkActionBlocker: false, handsComp: handsComponent);
                spawnedItems.Add(inhandEntity);
            }
        }

        return spawnedItems;
    }
}
