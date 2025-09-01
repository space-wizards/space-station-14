using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Inventory;
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
            if ((slot.SlotFlags | slotFlag) == 0) // Does this seem somewhat illegal? yes. Does C# provide an alternative function for checking if an enum has ANY of a set of bit flags? no.
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
        SlotFlags slotFlag) => InventoryPutEnumerableBase(ents, itemEnt, slotFlag, InventoryForcePut);


    public EntityUid? InventoryForcePut(EntityUid targetEnt, EntityUid itemEnt, SlotFlags slotFlag)
    {
        return InventoryPutBase(targetEnt,
            itemEnt,
            slotFlag,
            PutType.ForcePut) is not null
            ? targetEnt
            : null;
    }


    [CommandImplementation("put")]
    public EntityUid? InventoryPut([PipedArgument] IEnumerable<EntityUid> ents,
        EntityUid itemEnt,
        SlotFlags slotFlag) => InventoryPutEnumerableBase(ents, itemEnt, slotFlag, InventoryPut);


    public EntityUid? InventoryPut(EntityUid targetEnt, EntityUid itemEnt, SlotFlags slotFlag)
    {
        return InventoryPutBase(targetEnt,
            itemEnt,
            slotFlag,
            PutType.Put) is not null
            ? targetEnt
            : null;
    }


    [CommandImplementation("tryput")]
    public EntityUid? InventoryTryPut([PipedArgument] IEnumerable<EntityUid> ents,
        EntityUid itemEnt,
        SlotFlags slotFlag) => InventoryPutEnumerableBase(ents, itemEnt, slotFlag, InventoryTryPut);


    public EntityUid? InventoryTryPut(EntityUid targetEnt, EntityUid itemEnt, SlotFlags slotFlag)
    {
        return InventoryPutBase(targetEnt,
            itemEnt,
            slotFlag,
            PutType.TryPut) is not null
            ? targetEnt
            : null;
    }




    [CommandImplementation("ensure")]
    public EntityUid? InventoryEnsure([PipedArgument] IEnumerable<EntityUid> ents,
        EntityUid itemEnt,
        SlotFlags slotFlag) => InventoryPutEnumerableBase(ents, itemEnt, slotFlag, InventoryEnsure);

    public EntityUid? InventoryEnsure(EntityUid targetEnt, EntityUid itemEnt, SlotFlags slotFlag)
    {
        return InventoryPutBase(targetEnt,
            itemEnt,
            slotFlag,
            PutType.Ensure);
    }

    private EntityUid? InventoryPutEnumerableBase(IEnumerable<EntityUid> targetEnts,
        EntityUid itemToInsert,
        SlotFlags slotFlags,
        Func<EntityUid, EntityUid, SlotFlags, EntityUid?> targetFunc)
    {
        foreach (var entity in targetEnts)
        {
            var result = targetFunc(entity, itemToInsert, slotFlags);
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
            if ((slot.SlotFlags | slotFlag) == 0)
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
