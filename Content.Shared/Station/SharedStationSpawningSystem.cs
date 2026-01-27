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
// Public API Methods Handle Every Possible Type
    public void EquipStartingGear(EntityUid entity, LoadoutPrototype loadout, bool raiseEvent = true)
        => EquipStartingGear(entity, (object?)loadout, raiseEvent);

    public void EquipStartingGear(EntityUid entity, ProtoId<StartingGearPrototype>? startingGear,
        bool raiseEvent = true)
        => EquipStartingGear(entity, (object?)startingGear, raiseEvent);

    public void EquipStartingGear(EntityUid entity, StartingGearPrototype? startingGear,
        bool raiseEvent = true)
        => EquipStartingGear(entity, (object?)startingGear, raiseEvent);

    public void EquipStartingGear(EntityUid entity, IEquipmentLoadout? loadout,
        bool raiseEvent = true)
        => EquipStartingGear(entity, (object?)loadout, raiseEvent);

    private void EquipStartingGear(EntityUid entity, object? loadoutSpec, bool raiseEvent = true)
    {
        if (loadoutSpec is null) return;

        switch (loadoutSpec)
        {
            case LoadoutPrototype lp:
                if (lp.StartingGear != null)
                    EquipStartingGear(entity, lp.StartingGear, raiseEvent);
                _applyEquipment(entity, lp, raiseEvent);
                break;
            case ProtoId<StartingGearPrototype> protoId:
                if (PrototypeManager.TryIndex(protoId, out StartingGearPrototype? spProto))
                    _applyEquipment(entity, spProto, raiseEvent);
                else
                    Log.Error($"Failed to resolve StartingGearPrototype '{protoId}'.");
                break;
            case StartingGearPrototype sp:
                _applyEquipment(entity, sp, raiseEvent);
                break;
            case IEquipmentLoadout equipment:
                _applyEquipment(entity, equipment, raiseEvent);
                break;
            default:
                Log.Error($"Unsupported loadout type '{loadoutSpec.GetType()}' passed to EquipStartingGear.");
                break;
        }
    }
    private void _applyEquipment(EntityUid entity, IEquipmentLoadout? gear, bool raiseEvent = true)
    {
        if (gear == null) return;

        var xform = _xformQuery.GetComponent(entity);

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
    public void EquipRoleLoadout(EntityUid entity, RoleLoadout loadout, RoleLoadoutPrototype roleProto)
    {
        foreach (var group in loadout.SelectedLoadouts
                                   .OrderBy(x => roleProto.Groups.FindIndex(e => e == x.Key)))
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

    public void EquipRoleName(EntityUid entity, RoleLoadout loadout, RoleLoadoutPrototype roleProto)
    {
        string? name = null;

        if (roleProto.CanCustomizeName)
            name = loadout.EntityName;

        if (string.IsNullOrEmpty(name) && PrototypeManager.Resolve(roleProto.NameDataset, out var nameData))
            name = Loc.GetString(_random.Pick(nameData.Values));

        if (!string.IsNullOrEmpty(name))
            _metadata.SetEntityName(entity, name);
    }

    public string? GetGearForSlot(RoleLoadout? loadout, string slot)
    {
        if (loadout == null) return null;

        foreach (var group in loadout.SelectedLoadouts)
        {
            foreach (var items in group.Value)
            {
                if (!PrototypeManager.Resolve(items.Prototype, out var loadoutProto))
                    return null;

                var gear = ((IEquipmentLoadout)loadoutProto).GetGear(slot);
                if (gear != string.Empty) return gear;
            }
        }
        return null;
    }
}
