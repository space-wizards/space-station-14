using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;

namespace Content.Server.Inventory;

[ToolshedCommand, AdminCommand(AdminFlags.Debug)]
public sealed class InventoryCommand : ToolshedCommand
{
    private InventorySystem? _inventorySystem;

    [CommandImplementation("getflags")]
    public IEnumerable<EntityUid> InventoryGetFlags([PipedArgument] IEnumerable<EntityUid> ents, SlotFlags slotFlag)
    {
        var items = Enumerable.Empty<EntityUid>();
        foreach (var ent in ents)
        {
            items = items.Concat(InventoryGetFlags(ent, slotFlag));
        }

        return items;
    }

    public IEnumerable<EntityUid> InventoryGetFlags(EntityUid ent, SlotFlags slotFlag)
    {
        _inventorySystem ??= GetSys<InventorySystem>();

        if (!EntityManager.TryGetComponent<InventoryComponent>(ent, out var inventory))
            return [];

        List<EntityUid> items = new();

        foreach (var slot in inventory.Slots)
        {
            if ((slot.SlotFlags & slotFlag) == 0) // Does this seem somewhat illegal? yes. Does C# provide an alternative function for checking if an enum has ANY of a set of bit flags? no.
                continue;
            if (_inventorySystem.TryGetSlotEntity(ent, slot.Name, out var item, inventory))
                items.Add(item.Value);
        }

        return items;
    }


    [CommandImplementation("getnamed")]
    public IEnumerable<EntityUid> InventoryGetNamed([PipedArgument] IEnumerable<EntityUid> ents, string slotName)
    {
        var items = Enumerable.Empty<EntityUid>();
        foreach (var ent in ents)
        {
            items = items.Concat(InventoryGetNamed(ent, slotName));
        }

        return items;
    }

    public IEnumerable<EntityUid> InventoryGetNamed(EntityUid ent, string slotName)
    {
        _inventorySystem ??= GetSys<InventorySystem>();

        if (!EntityManager.TryGetComponent<InventoryComponent>(ent, out var inventory))
            return [];

        List<EntityUid> items = new();

        foreach (var slot in inventory.Slots)
        {
            if (slot.Name != slotName)
                continue;
            if (_inventorySystem.TryGetSlotEntity(ent, slot.Name, out var item, inventory))
                items.Add(item.Value);
        }

        return items;
    }

    [CommandImplementation("forceput")]
    public EntityUid? InventoryForcePut([PipedArgument] IEnumerable<EntityUid> ents,
        EntityUid itemEnt,
        SlotFlags slotFlag) => InventoryPutEnumerableBase(ents, itemEnt, slotFlag, PutType.ForcePut);
    [CommandImplementation("forcespawn")]
    public EntityUid? InventoryForceSpawn([PipedArgument] IEnumerable<EntityUid> ents,
        EntProtoId itemEnt,
        SlotFlags slotFlag) => InventorySpawnEnumerableBase(ents, itemEnt, slotFlag, PutType.ForcePut);

    [CommandImplementation("put")]
    public EntityUid? InventoryPut([PipedArgument] IEnumerable<EntityUid> ents,
        EntityUid itemEnt,
        SlotFlags slotFlag) => InventoryPutEnumerableBase(ents, itemEnt, slotFlag, PutType.Put);
    [CommandImplementation("spawn")]
    public EntityUid? InventorySpawn([PipedArgument] IEnumerable<EntityUid> ents,
        EntProtoId itemEnt,
        SlotFlags slotFlag) => InventorySpawnEnumerableBase(ents, itemEnt, slotFlag, PutType.Put);

    [CommandImplementation("tryput")]
    public EntityUid? InventoryTryPut([PipedArgument] IEnumerable<EntityUid> ents,
        EntityUid itemEnt,
        SlotFlags slotFlag) => InventoryPutEnumerableBase(ents, itemEnt, slotFlag, PutType.Put);
    [CommandImplementation("tryspawn")]
    public EntityUid? InventoryTrySpawn([PipedArgument] IEnumerable<EntityUid> ents,
        EntProtoId itemEnt,
        SlotFlags slotFlag) => InventorySpawnEnumerableBase(ents, itemEnt, slotFlag, PutType.Put);

    [CommandImplementation("ensure")]
    public EntityUid? InventoryEnsure([PipedArgument] IEnumerable<EntityUid> ents,
        EntityUid itemEnt,
        SlotFlags slotFlag) => InventoryPutEnumerableBase(ents, itemEnt, slotFlag, PutType.Ensure);
    [CommandImplementation("ensurespawn")]
    public EntityUid? InventoryEnsureSpawn([PipedArgument] IEnumerable<EntityUid> ents,
        EntProtoId itemEnt,
        SlotFlags slotFlag) => InventorySpawnEnumerableBase(ents, itemEnt, slotFlag, PutType.Ensure);


    private EntityUid? InventorySpawnEnumerableBase(IEnumerable<EntityUid> targetEnts,
        EntProtoId itemToInsert,
        SlotFlags slotFlags,
        PutType putType)
    {
        var entityUids = targetEnts as EntityUid[] ?? targetEnts.ToArray();
        if (!entityUids.Any())
            return null;

        var spawnedItem = Spawn(itemToInsert, Transform(entityUids.First()).Coordinates);

        foreach (var entity in entityUids)
        {
            var result = InventoryPutBase(entity, spawnedItem, slotFlags, putType);
            if (result == null)
                continue;
            if (!result.Value.Equals(spawnedItem)) Del(spawnedItem);
            return result;
        }
        Del(spawnedItem);
        return null;
    }
    private EntityUid? InventoryPutEnumerableBase(IEnumerable<EntityUid> targetEnts,
        EntityUid itemToInsert,
        SlotFlags slotFlags,
        PutType putType)
    {
        foreach (var entity in targetEnts)
        {
            var result = InventoryPutBase(entity, itemToInsert, slotFlags, putType);
            if (result != null)
                return result;
        }

        return null;
    }

    private EntityUid? InventoryPutBase(EntityUid targetEnt,
        EntityUid itemToInsert,
        SlotFlags slotFlag,
        PutType putType)
    {
        _inventorySystem ??= GetSys<InventorySystem>();

        if (!EntityManager.TryGetComponent<InventoryComponent>(targetEnt, out var inventory))
            return null;


        foreach (var slot in inventory.Slots)
        {
            if ((slot.SlotFlags & slotFlag) == 0)
                continue;


            if (_inventorySystem.TryGetSlotEntity(targetEnt, slot.Name, out var originalItem, inventory))
            {
                if (putType == PutType.ForcePut)
                    EntityManager.DeleteEntity(originalItem);
                if (putType == PutType.Put)
                {
                    if (!_inventorySystem.TryUnequip(targetEnt, slot.Name, force: true, inventory: inventory))
                        return null;
                }
            }

            if (_inventorySystem.TryEquip(targetEnt, itemToInsert, slot.Name, force: true, inventory: inventory))
                return itemToInsert;
            else
                return putType == PutType.Ensure ? originalItem : null;
        }

        return null;
    }

    private enum PutType
    {
        ForcePut, // Put item in slot, delete old item
        Put, // Put item in slot, put old item on floor
        TryPut, // Put item in slot, fail if there is already an item
        Ensure // Try put item in slot. If there is one, return the item already there
    }
}
