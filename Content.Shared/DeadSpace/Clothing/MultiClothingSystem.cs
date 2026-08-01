// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Linq;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared.DeadSpace.Clothing;

public sealed class MultiClothingSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly INetManager _net = default!;

    public const string ContainerId = "multi-clothing-container";

    private readonly Dictionary<EntityUid, DeferredHostRollbackEvent> _pendingHostRollbacks = new();
    private readonly Dictionary<EntityUid, DeferredCleanupEvent> _pendingCleanups = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MultiClothingComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MultiClothingComponent, EntityTerminatingEvent>(OnTerminating);
        SubscribeLocalEvent<MultiClothingComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<IsEquippingAttemptEvent>(OnEquippingAttempt);
        SubscribeLocalEvent<MultiClothingComponent, GotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<MultiClothingComponent, GotUnequippedEvent>(OnGotUnequipped);
        SubscribeLocalEvent<DeferredHostRollbackEvent>(OnDeferredHostRollback);
        SubscribeLocalEvent<DeferredCleanupEvent>(OnDeferredCleanup);
    }

    private void OnMapInit(Entity<MultiClothingComponent> ent, ref MapInitEvent args)
    {
        _container.EnsureContainer<Container>(ent, ContainerId);
    }

    private void OnShutdown(Entity<MultiClothingComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent.Owner))
            return;

        ReleaseEquipment(ent);
    }

    private void OnTerminating(Entity<MultiClothingComponent> ent, ref EntityTerminatingEvent args)
    {
        ReleaseTerminatingEquipment(ent);
    }

    private void ReleaseEquipment(Entity<MultiClothingComponent> ent)
    {
        _pendingHostRollbacks.Remove(ent.Owner);
        _pendingCleanups.Remove(ent.Owner);

        if (!_net.IsServer ||
            !_container.TryGetContainer(ent.Owner, ContainerId, out var baseContainer) ||
            baseContainer is not Container container)
        {
            return;
        }

        var equipee = Transform(ent).ParentUid;
        var hasInventory = HasComp<InventoryComponent>(equipee);
        var allSlots = ent.Comp.SpawnedItems.Keys
            .Concat(ent.Comp.ForcedOffItems.Keys)
            .ToHashSet();
        var preserved = new Dictionary<string, EntityUid>();

        if (hasInventory && TryGetAffectedSlots(equipee, ent.Comp.SpawnedItems.Keys, out var parentFirst))
        {
            foreach (var (slotName, itemUid) in CaptureItems(equipee, parentFirst))
            {
                if (!ent.Comp.SpawnedItems.ContainsKey(slotName))
                    preserved[slotName] = itemUid;
            }
        }

        if (!TryGetAffectedSlots(equipee, allSlots.Concat(preserved.Keys), out parentFirst))
            parentFirst = allSlots.Concat(preserved.Keys).Distinct().OrderBy(slot => slot).ToList();

        if (hasInventory)
            StoreItems(equipee, preserved, parentFirst, container);

        foreach (var slotName in parentFirst.AsEnumerable().Reverse())
        {
            if (!ent.Comp.SpawnedItems.TryGetValue(slotName, out var itemUid) || !Exists(itemUid))
                continue;

            RemComp<UnremoveableComponent>(itemUid);
            if (hasInventory)
                TryStoreItem(equipee, slotName, itemUid, container);
            QueueDel(itemUid);
        }

        var remainingForcedOff = hasInventory
            ? RestoreItems(equipee, ent.Comp.ForcedOffItems, parentFirst)
            : new Dictionary<string, EntityUid>(ent.Comp.ForcedOffItems);
        var remainingPreserved = hasInventory
            ? RestoreItems(equipee, preserved, parentFirst)
            : preserved;

        var dropTarget = hasInventory ? equipee : ent.Owner;
        var userItems = ent.Comp.ForcedOffItems.Values
            .Concat(preserved.Values)
            .ToHashSet();
        ReleaseItems(dropTarget, remainingForcedOff, container);
        ReleaseItems(dropTarget, remainingPreserved, container);

        var auxiliaryPrototypes = ent.Comp.Equipment.Values.Select(proto => proto.Id).ToHashSet();
        foreach (var itemUid in container.ContainedEntities.ToArray())
        {
            if (!Exists(itemUid))
                continue;

            var prototype = MetaData(itemUid).EntityPrototype?.ID;
            if (!userItems.Contains(itemUid) && prototype != null && auxiliaryPrototypes.Contains(prototype))
            {
                QueueDel(itemUid);
                continue;
            }

            if (_container.Remove(itemUid, container, force: true))
                _transform.DropNextTo(itemUid, dropTarget);
        }

        ent.Comp.SpawnedItems.Clear();
        ent.Comp.ForcedOffItems.Clear();
    }

    private void ReleaseTerminatingEquipment(Entity<MultiClothingComponent> ent)
    {
        _pendingHostRollbacks.Remove(ent.Owner);
        _pendingCleanups.Remove(ent.Owner);

        if (!_net.IsServer ||
            !_container.TryGetContainer(ent.Owner, ContainerId, out var baseContainer) ||
            baseContainer is not Container container)
        {
            return;
        }

        var equipee = Transform(ent).ParentUid;
        var hasInventory = HasComp<InventoryComponent>(equipee);
        var allSlots = ent.Comp.SpawnedItems.Keys
            .Concat(ent.Comp.ForcedOffItems.Keys)
            .ToHashSet();
        var preserved = new Dictionary<string, EntityUid>();

        if (hasInventory && TryGetAffectedSlots(equipee, ent.Comp.SpawnedItems.Keys, out var parentFirst))
        {
            foreach (var (slotName, itemUid) in CaptureItems(equipee, parentFirst))
            {
                if (!ent.Comp.SpawnedItems.ContainsKey(slotName))
                    preserved[slotName] = itemUid;
            }
        }

        if (!TryGetAffectedSlots(equipee, allSlots.Concat(preserved.Keys), out parentFirst))
            parentFirst = allSlots.Concat(preserved.Keys).Distinct().OrderBy(slot => slot).ToList();

        foreach (var slotName in parentFirst.AsEnumerable().Reverse())
        {
            if (!ent.Comp.SpawnedItems.TryGetValue(slotName, out var itemUid) || !Exists(itemUid))
                continue;

            RemComp<UnremoveableComponent>(itemUid);
            if (hasInventory &&
                _inventory.TryGetSlotEntity(equipee, slotName, out var equipped) &&
                equipped == itemUid)
            {
                _inventory.TryUnequip(equipee,
                    slotName,
                    predicted: true,
                    silent: true,
                    force: true);
            }

            QueueDel(itemUid);
        }

        var remainingForcedOff = hasInventory
            ? RestoreItems(equipee, ent.Comp.ForcedOffItems, parentFirst)
            : new Dictionary<string, EntityUid>(ent.Comp.ForcedOffItems);
        var remainingPreserved = hasInventory
            ? RestoreItems(equipee, preserved, parentFirst)
            : preserved;

        var dropTarget = hasInventory ? equipee : ent.Owner;
        ReleaseItems(dropTarget, remainingForcedOff, container);
        ReleaseItems(dropTarget, remainingPreserved, container);

        ent.Comp.SpawnedItems.Clear();
        ent.Comp.ForcedOffItems.Clear();
    }

    private void OnEquippingAttempt(IsEquippingAttemptEvent args)
    {
        if (!TryComp<MultiClothingComponent>(args.Equipment, out var component))
            return;

        foreach (var (slotName, _) in component.Equipment)
        {
            if (!_inventory.TryGetSlot(args.EquipTarget, slotName, out _))
            {
                args.Cancel();
                return;
            }

            if (!component.Force && _inventory.TryGetSlotEntity(args.EquipTarget, slotName, out _))
            {
                args.Cancel();
                return;
            }
        }
    }

    private void OnGotEquipped(Entity<MultiClothingComponent> ent, ref GotEquippedEvent args)
    {
        if (!_net.IsServer)
            return;

        _pendingHostRollbacks.Remove(ent.Owner);

        var container = _container.EnsureContainer<Container>(ent, ContainerId);
        var hostSlot = args.Slot;
        var targetSlots = ent.Comp.Equipment.Keys
            .Where(slot => slot != hostSlot)
            .ToHashSet();

        if (targetSlots.Count == 0)
            return;

        if (ent.Comp.SpawnedItems.Count != 0 || ent.Comp.ForcedOffItems.Count != 0 ||
            !TryGetAffectedSlots(args.Equipee, targetSlots, out var parentFirst) ||
            parentFirst.Contains(hostSlot))
        {
            ScheduleHostRollback(ent.Owner, args.Equipee, args.Slot);
            return;
        }

        var displaced = CaptureItems(args.Equipee, parentFirst);
        var equipped = new Dictionary<string, EntityUid>();

        if ((!ent.Comp.Force && targetSlots.Any(displaced.ContainsKey)) ||
            displaced.Values.Any(HasComp<UnremoveableComponent>))
        {
            ScheduleHostRollback(ent.Owner, args.Equipee, args.Slot);
            return;
        }

        if (!StoreItems(args.Equipee, displaced, parentFirst, container))
        {
            Rollback(ent, args.Equipee, equipped, displaced, parentFirst, container);
            ScheduleHostRollback(ent.Owner, args.Equipee, args.Slot);
            return;
        }

        var reserved = displaced.Values.ToHashSet();
        foreach (var slotName in parentFirst.Where(targetSlots.Contains))
        {
            var proto = ent.Comp.Equipment[slotName];
            var item = container.ContainedEntities.FirstOrDefault(entity =>
                !reserved.Contains(entity) && MetaData(entity).EntityPrototype?.ID == proto.Id);
            var spawned = item == default;

            if (spawned)
                item = Spawn(proto, Transform(ent).Coordinates);

            if (!_inventory.TryEquip(args.Equipee,
                    item,
                    slotName,
                    predicted: true,
                    silent: true,
                    force: true))
            {
                if (Exists(item) && !container.Contains(item) && !_container.Insert(item, container) && spawned)
                    QueueDel(item);

                Rollback(ent, args.Equipee, equipped, displaced, parentFirst, container);
                ScheduleHostRollback(ent.Owner, args.Equipee, args.Slot);
                return;
            }

            EnsureComp<UnremoveableComponent>(item);
            equipped[slotName] = item;
        }

        var preserved = displaced
            .Where(pair => !targetSlots.Contains(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        if (RestoreItems(args.Equipee, preserved, parentFirst).Count != 0)
        {
            Rollback(ent, args.Equipee, equipped, displaced, parentFirst, container);
            ScheduleHostRollback(ent.Owner, args.Equipee, args.Slot);
            return;
        }

        ent.Comp.SpawnedItems = equipped;
        ent.Comp.ForcedOffItems = displaced
            .Where(pair => targetSlots.Contains(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        Dirty(ent);
    }

    private void OnGotUnequipped(Entity<MultiClothingComponent> ent, ref GotUnequippedEvent args)
    {
        if (!_net.IsServer)
            return;

        _pendingHostRollbacks.Remove(ent.Owner);

        if (ent.Comp.SpawnedItems.Count == 0 && ent.Comp.ForcedOffItems.Count == 0)
            return;

        var preserved = new Dictionary<string, EntityUid>();
        if (TryGetAffectedSlots(args.Equipee, ent.Comp.SpawnedItems.Keys, out var parentFirst))
        {
            foreach (var (slot, item) in CaptureItems(args.Equipee, parentFirst))
            {
                if (!ent.Comp.SpawnedItems.ContainsKey(slot))
                    preserved[slot] = item;
            }
        }

        var cleanup = new DeferredCleanupEvent(ent.Owner, args.Equipee, preserved);
        _pendingCleanups[ent.Owner] = cleanup;
        QueueLocalEvent(cleanup);
    }

    private void Rollback(
        Entity<MultiClothingComponent> ent,
        EntityUid equipee,
        Dictionary<string, EntityUid> equipped,
        Dictionary<string, EntityUid> displaced,
        List<string> parentFirst,
        Container container)
    {
        StoreItems(equipee, displaced, parentFirst, container);

        var remainingSpawned = new Dictionary<string, EntityUid>();
        foreach (var slotName in parentFirst.AsEnumerable().Reverse())
        {
            if (!equipped.TryGetValue(slotName, out var itemUid))
                continue;

            if (!Exists(itemUid))
                continue;

            RemComp<UnremoveableComponent>(itemUid);
            if (!TryStoreItem(equipee, slotName, itemUid, container))
                remainingSpawned[slotName] = itemUid;
        }

        ent.Comp.SpawnedItems = remainingSpawned;
        ent.Comp.ForcedOffItems = RestoreItems(equipee, displaced, parentFirst);
        Dirty(ent);
    }

    private void ScheduleHostRollback(EntityUid host, EntityUid equipee, string slot)
    {
        var rollback = new DeferredHostRollbackEvent(host, equipee, slot);
        _pendingHostRollbacks[host] = rollback;
        QueueLocalEvent(rollback);
    }

    private void OnDeferredHostRollback(DeferredHostRollbackEvent args)
    {
        if (!_pendingHostRollbacks.TryGetValue(args.Host, out var active) || !ReferenceEquals(active, args))
            return;

        _pendingHostRollbacks.Remove(args.Host);

        if (TerminatingOrDeleted(args.Host) ||
            TerminatingOrDeleted(args.Equipee) ||
            !HasComp<MultiClothingComponent>(args.Host) ||
            !_inventory.TryGetSlotEntity(args.Equipee, args.Slot, out var current) ||
            current != args.Host)
        {
            return;
        }

        _inventory.TryUnequip(args.Equipee,
            args.Slot,
            predicted: true,
            silent: true,
            force: true);
    }

    private void OnDeferredCleanup(DeferredCleanupEvent args)
    {
        if (!_pendingCleanups.TryGetValue(args.Host, out var active) || !ReferenceEquals(active, args))
            return;

        _pendingCleanups.Remove(args.Host);

        if (TerminatingOrDeleted(args.Host) ||
            TerminatingOrDeleted(args.Equipee) ||
            !TryComp<MultiClothingComponent>(args.Host, out var component))
        {
            return;
        }

        var container = _container.EnsureContainer<Container>(args.Host, ContainerId);
        var allSlots = component.SpawnedItems.Keys
            .Concat(component.ForcedOffItems.Keys)
            .Concat(args.PreservedItems.Keys)
            .ToHashSet();

        if (!TryGetAffectedSlots(args.Equipee, allSlots, out var parentFirst))
            parentFirst = allSlots.OrderBy(slot => slot).ToList();

        StoreItems(args.Equipee, args.PreservedItems, parentFirst, container);

        var remainingSpawned = new Dictionary<string, EntityUid>();
        foreach (var slotName in parentFirst.AsEnumerable().Reverse())
        {
            if (!component.SpawnedItems.TryGetValue(slotName, out var itemUid))
                continue;

            if (!Exists(itemUid))
                continue;

            RemComp<UnremoveableComponent>(itemUid);
            if (!TryStoreItem(args.Equipee, slotName, itemUid, container))
                remainingSpawned[slotName] = itemUid;
        }

        var remainingForcedOff = RestoreItems(args.Equipee, component.ForcedOffItems, parentFirst);
        var remainingPreserved = RestoreItems(args.Equipee, args.PreservedItems, parentFirst);
        ReleaseItems(args.Equipee, remainingPreserved, container);

        component.SpawnedItems = remainingSpawned;
        component.ForcedOffItems = remainingForcedOff;
        Dirty(args.Host, component);
    }

    private Dictionary<string, EntityUid> CaptureItems(EntityUid equipee, IEnumerable<string> slots)
    {
        var items = new Dictionary<string, EntityUid>();
        foreach (var slotName in slots)
        {
            if (_inventory.TryGetSlotEntity(equipee, slotName, out var item))
                items[slotName] = item.Value;
        }

        return items;
    }

    private bool StoreItems(
        EntityUid equipee,
        IReadOnlyDictionary<string, EntityUid> items,
        List<string> parentFirst,
        Container container)
    {
        var success = true;
        foreach (var slotName in parentFirst.AsEnumerable().Reverse())
        {
            if (items.TryGetValue(slotName, out var itemUid) &&
                !TryStoreItem(equipee, slotName, itemUid, container))
            {
                success = false;
            }
        }

        return success;
    }

    private bool TryStoreItem(EntityUid equipee, string slotName, EntityUid itemUid, Container container)
    {
        if (!Exists(itemUid))
            return false;

        if (container.Contains(itemUid))
            return true;

        if (_inventory.TryGetSlotEntity(equipee, slotName, out var equipped) && equipped == itemUid &&
            !_inventory.TryUnequip(equipee,
                slotName,
                predicted: true,
                silent: true,
                force: true))
        {
            return false;
        }

        return container.Contains(itemUid) || _container.Insert(itemUid, container);
    }

    private void ReleaseItems(
        EntityUid dropTarget,
        IReadOnlyDictionary<string, EntityUid> items,
        Container container)
    {
        foreach (var itemUid in items.Values)
        {
            if (!Exists(itemUid))
                continue;

            if (container.Contains(itemUid) && !_container.Remove(itemUid, container, force: true))
                continue;

            if (!TerminatingOrDeleted(itemUid))
                _transform.DropNextTo(itemUid, dropTarget);
        }
    }

    private Dictionary<string, EntityUid> RestoreItems(
        EntityUid equipee,
        IReadOnlyDictionary<string, EntityUid> items,
        List<string> parentFirst)
    {
        var remaining = new Dictionary<string, EntityUid>(items);
        foreach (var slotName in parentFirst)
        {
            if (!items.TryGetValue(slotName, out var itemUid))
                continue;

            if (!Exists(itemUid) || TryRestoreItem(equipee, slotName, itemUid))
                remaining.Remove(slotName);
        }

        return remaining;
    }

    private bool TryRestoreItem(EntityUid equipee, string slotName, EntityUid itemUid)
    {
        if (_inventory.TryGetSlotEntity(equipee, slotName, out var current))
            return current == itemUid;

        if (!_inventory.TryGetSlot(equipee, slotName, out var slot) ||
            slot.DependsOn != null && !_inventory.TryGetSlotEntity(equipee, slot.DependsOn, out _))
        {
            return false;
        }

        return _inventory.TryEquip(equipee,
            itemUid,
            slotName,
            predicted: true,
            silent: true,
            force: true);
    }

    private bool TryGetAffectedSlots(
        EntityUid equipee,
        IEnumerable<string> roots,
        out List<string> parentFirst)
    {
        parentFirst = new List<string>();
        if (!_inventory.TryGetSlots(equipee, out var slots))
            return false;

        var byName = slots.ToDictionary(slot => slot.Name);
        var affected = roots.ToHashSet();
        if (affected.Any(root => !byName.ContainsKey(root)))
            return false;

        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var slot in slots)
            {
                if (slot.DependsOn != null && affected.Contains(slot.DependsOn))
                    changed |= affected.Add(slot.Name);
            }
        }

        parentFirst = affected
            .OrderBy(slot => GetSlotDepth(slot, byName))
            .ThenBy(slot => slot)
            .ToList();
        return true;
    }

    private static int GetSlotDepth(string slotName, IReadOnlyDictionary<string, SlotDefinition> slots)
    {
        var depth = 0;
        var visited = new HashSet<string>();
        while (slots.TryGetValue(slotName, out var slot) &&
               slot.DependsOn is { } parent &&
               visited.Add(slotName))
        {
            depth++;
            slotName = parent;
        }

        return depth;
    }

    private sealed class DeferredHostRollbackEvent(EntityUid host, EntityUid equipee, string slot) : EntityEventArgs
    {
        public readonly EntityUid Host = host;
        public readonly EntityUid Equipee = equipee;
        public readonly string Slot = slot;
    }

    private sealed class DeferredCleanupEvent(
        EntityUid host,
        EntityUid equipee,
        Dictionary<string, EntityUid> preservedItems) : EntityEventArgs
    {
        public readonly EntityUid Host = host;
        public readonly EntityUid Equipee = equipee;
        public readonly Dictionary<string, EntityUid> PreservedItems = preservedItems;
    }
}
