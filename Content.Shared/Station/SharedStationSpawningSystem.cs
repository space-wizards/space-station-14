using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Content.Shared.Roles;

namespace Content.Shared.Station;

public abstract class SharedStationSpawningSystem : EntitySystem
{
    [Dependency] protected readonly InventorySystem InventorySystem = default!;
    [Dependency] private   readonly SharedHandsSystem _handsSystem = default!;

    /// <summary>
    /// Equips starting gear onto the given entity.
    /// </summary>
    /// <param name="entity">Entity to load out.</param>
    /// <param name="startingGear">Starting gear to use.</param>
    /// <param name="profile">Character profile to use, if any.</param>
    public void EquipStartingGear(EntityUid entity, StartingGearPrototype startingGear, HumanoidCharacterProfile? profile)
    {
        if (InventorySystem.TryGetSlots(entity, out var slotDefinitions))
        {
            foreach (var slot in slotDefinitions)
            {
                var equipmentStr = startingGear.GetGear(slot.Name, profile);
                if (!string.IsNullOrEmpty(equipmentStr))
                {
                    var equipmentEntity = EntityManager.SpawnEntity(equipmentStr, EntityManager.GetComponent<TransformComponent>(entity).Coordinates);
                    InventorySystem.TryEquip(entity, equipmentEntity, slot.Name, true);
                }
            }
        }

        if (!TryComp(entity, out HandsComponent? handsComponent))
            return;

        var inhand = startingGear.Inhand;
        var coords = EntityManager.GetComponent<TransformComponent>(entity).Coordinates;
        foreach (var (hand, prototype) in inhand)
        {
            var inhandEntity = EntityManager.SpawnEntity(prototype, coords);
            _handsSystem.TryPickup(entity, inhandEntity, hand, checkActionBlocker: false, handsComp: handsComponent);
        }
    }
}
