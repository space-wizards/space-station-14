using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Collections;

namespace Content.Shared.Station;

public abstract class SharedStationSpawningSystem : EntitySystem
{
    [Dependency] protected readonly InventorySystem InventorySystem = default!;
    [Dependency] private   readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private   readonly SharedStorageSystem _storage = default!;
    [Dependency] private   readonly SharedTransformSystem _xformSystem = default!;

    /// <summary>
    /// Equips starting gear onto the given entity.
    /// </summary>
    /// <param name="entity">Entity to load out.</param>
    /// <param name="startingGear">Starting gear to use.</param>
    public void EquipStartingGear(EntityUid entity, StartingGearPrototype startingGear)
    {
        if (InventorySystem.TryGetSlots(entity, out var slotDefinitions))
        {
            foreach (var slot in slotDefinitions)
            {
                var equipmentStr = startingGear.GetGear(slot.Name);
                if (!string.IsNullOrEmpty(equipmentStr))
                {
                    var equipmentEntity = EntityManager.SpawnEntity(equipmentStr, EntityManager.GetComponent<TransformComponent>(entity).Coordinates);
                    InventorySystem.TryEquip(entity, equipmentEntity, slot.Name, silent: true, force:true);
                }
            }
        }

        if (TryComp(entity, out HandsComponent? handsComponent))
        {
            var inhand = startingGear.Inhand;
            var coords = EntityManager.GetComponent<TransformComponent>(entity).Coordinates;
            foreach (var prototype in inhand)
            {
                var inhandEntity = EntityManager.SpawnEntity(prototype, coords);

                if (_handsSystem.TryGetEmptyHand(entity, out var emptyHand, handsComponent))
                {
                    _handsSystem.TryPickup(entity, inhandEntity, emptyHand, checkActionBlocker: false, handsComp: handsComponent);
                }
            }
        }

        if (startingGear.Storage.Count > 0)
        {
            var coords = _xformSystem.GetMapCoordinates(entity);
            var ents = new ValueList<EntityUid>();
            TryComp(entity, out InventoryComponent? inventoryComp);

            foreach (var (slot, entProtos) in startingGear.Storage)
            {
                if (entProtos.Count == 0)
                    continue;

                foreach (var ent in entProtos)
                {
                    ents.Add(Spawn(ent, coords));
                }

                if (inventoryComp != null &&
                    InventorySystem.TryGetSlotEntity(entity, slot, out var slotEnt, inventoryComponent: inventoryComp) &&
                    TryComp(slotEnt, out StorageComponent? storage))
                {
                    foreach (var ent in ents)
                    {
                        _storage.Insert(slotEnt.Value, ent, out _, storageComp: storage, playSound: false);
                    }
                }
            }
        }
    }
}
