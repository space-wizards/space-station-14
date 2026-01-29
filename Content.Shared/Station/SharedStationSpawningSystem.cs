using System.Linq;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;


namespace Content.Shared.Station;

public abstract class SharedStationSpawningSystem : EntitySystem
{
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] protected readonly InventorySystem InventorySystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;

    private EntityQuery<HandsComponent> _handsQuery;
    private EntityQuery<InventoryComponent> _inventoryQuery;
    private EntityQuery<StorageComponent> _storageQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();
        _handsQuery = GetEntityQuery<HandsComponent>();
        _inventoryQuery = GetEntityQuery<InventoryComponent>();
        _storageQuery = GetEntityQuery<StorageComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();
    }

    /// <summary>
    ///     Equips the data from a `RoleLoadout` onto an entity.
    /// </summary>
    public void EquipRoleLoadout(EntityUid entity, RoleLoadout loadout, RoleLoadoutPrototype roleProto)
    {
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

                EquipStartingGear(entity, loadoutProto, raiseEvent: false);
            }
        }

        EquipRoleName(entity, loadout, roleProto);
    }

    /// <summary>
    /// Applies the role's name as applicable to the entity.
    /// </summary>
    public void EquipRoleName(EntityUid entity, RoleLoadout loadout, RoleLoadoutPrototype roleProto)
    {
        string? name = null;

        if (roleProto.CanCustomizeName)
        {
            name = loadout.EntityName;
        }

        if (string.IsNullOrEmpty(name) && PrototypeManager.Resolve(roleProto.NameDataset, out var nameData))
        {
            name = Loc.GetString(_random.Pick(nameData.Values));
        }

        if (!string.IsNullOrEmpty(name))
        {
            _metadata.SetEntityName(entity, name);
        }
    }

    // Public API Methods Handle Every Possible Type
    public void EquipStartingGear(EntityUid entity, IEquipmentLoadout? gear, bool raiseEvent = true)
    {
        if (gear == null) return;

        var xform = Transform(entity);


        // inventory
        if (InventorySystem.TryGetSlots(entity, out var slotDefs))
        {
            foreach (var slot in slotDefs)
            {
                var protoId = gear.GetGear(slot.Name);
                if (string.IsNullOrEmpty(protoId)) continue;

                var spawned = Spawn(protoId, xform.Coordinates);
                InventorySystem.TryEquip(entity, spawned, slot.Name, silent: true, force: true);
            }
        }

        // hands
        if (_handsQuery.TryComp(entity, out var handsComp))
        {
            var coords = xform.Coordinates;
            foreach (var prototype in gear.Inhand)
            {
                var handEnt = Spawn(prototype, coords);
                if (_handsSystem.TryGetEmptyHand((entity, handsComp), out var emptyHand))
                {
                    _handsSystem.TryPickup(entity, handEnt, emptyHand,
                                           checkActionBlocker: false,
                                           handsComp: handsComp);
                }
            }
        }

        // storage
        if (gear.Storage.Count > 0 && _inventoryQuery.TryComp(entity, out var invComp))
        {
            var mapCoords = _xformSystem.GetMapCoordinates(entity);
            foreach (var (slotName, prototypes) in gear.Storage)
            {
                if (prototypes == null || prototypes.Count == 0) continue;

                if (InventorySystem.TryGetSlotEntity(entity, slotName,
                                                     out var slotEnt,
                                                     inventoryComponent: invComp) &&
                    _storageQuery.TryComp(slotEnt, out var storageComp))
                {
                    foreach (var proto in prototypes)
                    {
                        var spawned = Spawn(proto, mapCoords);
                        _storage.Insert(slotEnt.Value, spawned, out _, storageComp: storageComp,
                                        playSound: false);
                    }
                }
            }
        }
        if (raiseEvent)
        {
            var ev = new StartingGearEquippedEvent(entity);
            RaiseLocalEvent(entity, ref ev);
        }
    }

    /// <summary>
    ///     Gets all the gear for a given slot when passed a loadout.
    /// </summary>
    /// <param name="loadout">The loadout to look through.</param>
    /// <param name="slot">The slot that you want the clothing for.</param>
    /// <returns>
    ///     If there is a value for the given slot, it will return the proto id for that slot.
    ///     If nothing was found, will return null
    /// </returns>
    public string? GetGearForSlot(RoleLoadout? loadout, string slot)
    {
        if (loadout == null)
            return null;

        foreach (var group in loadout.SelectedLoadouts)
        {
            foreach (var items in group.Value)
            {
                if (!PrototypeManager.Resolve(items.Prototype, out var loadoutPrototype))
                    return null;

                var gear = ((IEquipmentLoadout) loadoutPrototype).GetGear(slot);
                if (gear != string.Empty)
                    return gear;
            }
        }

        return null;
    }
}
