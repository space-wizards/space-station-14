using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Roles;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Clothing;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared.Station;

public abstract class SharedStationSpawningSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] protected readonly InventorySystem InventorySystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;

    /// <summary>
    /// Equips starting gear onto the given entity.
    /// </summary>
    /// <param name="entity">Entity to load out.</param>
    /// <param name="startingGear">Starting gear to use.</param>
    public void EquipStartingGear(EntityUid entity, ProtoId<StartingGearPrototype>? startingGear)
    {
        PrototypeManager.TryIndex(startingGear, out var gearProto);
        EquipStartingGear(entity, gearProto);
    }

    /// <summary>
    /// Equips starting gear onto the given entity.
    /// </summary>
    /// <param name="entity">Entity to load out.</param>
    /// <param name="startingGear">Starting gear to use.</param>
    public void EquipStartingGear(EntityUid entity, StartingGearPrototype? startingGear, HumanoidCharacterProfile? profile = null)
    {
        if (startingGear == null)
            return;

        // Loadouts
        if (startingGear.Loadout != string.Empty)
        {
            var jobLoadout = LoadoutSystem.GetJobPrototype(startingGear.Loadout);

            if (PrototypeManager.TryIndex(jobLoadout, out RoleLoadoutPrototype? roleProto))
            {
                RoleLoadout? loadout = null;
                profile?.Loadouts.TryGetValue(jobLoadout, out loadout);

                // Set to default if not present
                if (loadout == null)
                {
                    loadout = new RoleLoadout(jobLoadout);
                    loadout.SetDefault(PrototypeManager);
                }

                // Order loadout selections by the order they appear on the prototype.
                foreach (var group in loadout.SelectedLoadouts.OrderBy(x => roleProto.Groups.FindIndex(e => e == x.Key)))
                {
                    foreach (var items in group.Value)
                    {
                        if (!PrototypeManager.TryIndex(items.Prototype, out var loadoutProto))
                        {
                            Log.Error($"Unable to find loadout prototype for {items.Prototype}");
                            continue;
                        }

                        if (!PrototypeManager.TryIndex(loadoutProto.Equipment, out var loadoutGear))
                        {
                            Log.Error($"Unable to find starting gear {loadoutProto.Equipment} for loadout {loadoutProto}");
                            continue;
                        }

                        // Handle any extra data here.
                        EquipStartingGear(entity, loadoutGear);
                    }
                }
            }
        }

        if (InventorySystem.TryGetSlots(entity, out var slotDefinitions))
        {
            foreach (var slot in slotDefinitions)
            {
                var equipmentStr = startingGear.GetGear(slot.Name);
                if (!string.IsNullOrEmpty(equipmentStr))
                {
                    var equipmentEntity = EntityManager.SpawnEntity(equipmentStr, EntityManager.GetComponent<TransformComponent>(entity).Coordinates);
                    InventorySystem.TryEquip(entity, equipmentEntity, slot.Name, silent: true, force: true);
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

                if (inventoryComp != null &&
                    InventorySystem.TryGetSlotEntity(entity, slot, out var slotEnt, inventoryComponent: inventoryComp) &&
                    TryComp(slotEnt, out StorageComponent? storage))
                {
                    foreach (var ent in entProtos)
                        ents.Add(Spawn(ent, coords));

                    foreach (var ent in ents)
                        _storage.Insert(slotEnt.Value, ent, out _, storageComp: storage, playSound: false);
                }
            }
        }
    }
}
