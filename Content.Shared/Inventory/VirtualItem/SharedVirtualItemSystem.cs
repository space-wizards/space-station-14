using System.Diagnostics.CodeAnalysis;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared.Inventory.VirtualItem;

/// <summary>
/// In charge of managing virtual items.
/// Virtual items are used to block a <see cref="SlotButton"/>
/// or a <see cref="HandButton"/> with a non-existent item that
/// is a visual copy of another for whatever use
/// </summary>
/// <remarks>
/// The slot visuals are managed by <see cref="HandsUiController"/>
/// and <see cref="InventoryUiController"/>, see the <see cref="VirtualItemComponent"/>
/// references there for more information
/// </remarks>
public abstract class SharedVirtualItemSystem : EntitySystem
{
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedItemSystem _itemSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;

    [ValidatePrototypeId<EntityPrototype>]
    private const string VirtualItem = "VirtualItem";

    public override void Initialize()
    {
        SubscribeLocalEvent<VirtualItemComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);

        SubscribeLocalEvent<VirtualItemComponent, BeingEquippedAttemptEvent>(OnBeingEquippedAttempt);
        SubscribeLocalEvent<VirtualItemComponent, BeingUnequippedAttemptEvent>(OnBeingUnequippedAttempt);

        SubscribeLocalEvent<VirtualItemComponent, BeforeRangedInteractEvent>(OnBeforeRangedInteract);
    }

    /// <summary>
    /// Updates the GUI buttons with the new entity.
    /// </summary>
    private void OnAfterAutoHandleState(Entity<VirtualItemComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_containerSystem.IsEntityInContainer(ent))
            _itemSystem.VisualsChanged(ent);
    }

    private void OnBeingEquippedAttempt(Entity<VirtualItemComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        // No interactions with a virtual item, please.
        args.Cancel();
    }

    private void OnBeingUnequippedAttempt(Entity<VirtualItemComponent> ent, ref BeingUnequippedAttemptEvent args)
    {
        // No interactions with a virtual item, please.
        args.Cancel();
    }

    private void OnBeforeRangedInteract(Entity<VirtualItemComponent> ent, ref BeforeRangedInteractEvent args)
    {
        // No interactions with a virtual item, please.
        args.Handled = true;
    }

    #region Hands
    /// <summary>
    /// Spawns a virtual item in a empty hand
    /// </summary>
    /// <param name="blockingEnt">The entity we will make a virtual entity copy of</param>
    /// <param name="user">The entity that we want to insert the virtual entity</param>
    public bool TrySpawnVirtualItemInHand(EntityUid blockingEnt, EntityUid user)
    {
        return TrySpawnVirtualItemInHand(blockingEnt, user, out _);
    }

    /// <inheritdoc cref="TrySpawnVirtualItemInHand(Robust.Shared.GameObjects.EntityUid,Robust.Shared.GameObjects.EntityUid)"/>
    public bool TrySpawnVirtualItemInHand(EntityUid blockingEnt, EntityUid user, [NotNullWhen(true)] out EntityUid? virtualItem)
    {
        if (!TrySpawnVirtualItem(blockingEnt, user, out virtualItem) || !_handsSystem.TryGetEmptyHand(user, out var hand))
            return false;

        _handsSystem.DoPickup(user, hand, virtualItem.Value);
        return true;
    }

    /// <summary>
    /// Scan the user's hands until we find the virtual entity, if the
    /// virtual entity is a copy of the matching entity, delete it
    /// </summary>
    public void DeleteInHandsMatching(EntityUid user, EntityUid matching)
    {
        // Client can't currently predict deleting networked entities so we use this workaround, another
        // problem can popup when the hands leave PVS for example and this avoids that too
        if (_netManager.IsClient)
            return;

        foreach (var hand in _handsSystem.EnumerateHands(user))
        {
            if (TryComp(hand.HeldEntity, out VirtualItemComponent? virt) && virt.BlockingEntity == matching)
            {
                DeleteVirtualItem((hand.HeldEntity.Value, virt), user);
            }
        }
    }
    #endregion

    #region Inventory

    /// <summary>
    /// Spawns a virtual item inside a inventory slot
    /// </summary>
    /// <param name="blockingEnt">The entity we will make a virtual entity copy of</param>
    /// <param name="user">The entity that we want to insert the virtual entity</param>
    /// <param name="slot">The slot to which we will insert the virtual entity (could be the "shoes" slot, for example)</param>
    public bool TrySpawnVirtualItemInInventory(EntityUid blockingEnt, EntityUid user, string slot, bool force = false)
    {
        return TrySpawnVirtualItemInInventory(blockingEnt, user, slot, force, out _);
    }

    /// <inheritdoc cref="TrySpawnVirtualItemInInventory(Robust.Shared.GameObjects.EntityUid,Robust.Shared.GameObjects.EntityUid,string,bool)"/>
    public bool TrySpawnVirtualItemInInventory(EntityUid blockingEnt, EntityUid user, string slot, bool force, [NotNullWhen(true)] out EntityUid? virtualItem)
    {
        if (!TrySpawnVirtualItem(blockingEnt, user, out virtualItem))
            return false;

        _inventorySystem.TryEquip(user, virtualItem.Value, slot, force: force);
        return true;
    }

    /// <summary>
    /// Scan the user's inventory slots until we find a virtual entity, when
    /// that's done check if the found virtual entity is a copy of our matching entity,
    /// if it is, delete it
    /// </summary>
    /// <param name="slotName">Set this param if you have the name of the slot, it avoids unnecessary queries</param>
    public void DeleteInSlotMatching(EntityUid user, EntityUid matching, string? slotName = null)
    {
        // Client can't currently predict deleting networked entities so we use this workaround, another
        // problem can popup when the hands leave PVS for example and this avoids that too
        if (_netManager.IsClient)
            return;

        if (slotName != null)
        {
            if (!_inventorySystem.TryGetSlotEntity(user, slotName, out var slotEnt))
                return;

            if (TryComp(slotEnt, out VirtualItemComponent? virt) && virt.BlockingEntity == matching)
                DeleteVirtualItem((slotEnt.Value, virt), user);

            return;
        }

        if (!_inventorySystem.TryGetSlots(user, out var slotDefinitions))
            return;

        foreach (var slot in slotDefinitions)
        {
            if (!_inventorySystem.TryGetSlotEntity(user, slot.Name, out var slotEnt))
                continue;

            if (TryComp(slotEnt, out VirtualItemComponent? virt) && virt.BlockingEntity == matching)
                DeleteVirtualItem((slotEnt.Value, virt), user);
        }
    }
    #endregion

    /// <summary>
    /// Spawns a virtual item and setups the component without any special handling
    /// </summary>
    /// <param name="blockingEnt">The entity we will make a virtual entity copy of</param>
    /// <param name="user">The entity that we want to insert the virtual entity</param>
    public bool TrySpawnVirtualItem(EntityUid blockingEnt, EntityUid user, [NotNullWhen(true)] out EntityUid? virtualItem)
    {
        if (_netManager.IsClient)
        {
            virtualItem = null;
            return false;
        }

        var pos = Transform(user).Coordinates;
        virtualItem = Spawn(VirtualItem, pos);
        var virtualItemComp = Comp<VirtualItemComponent>(virtualItem.Value);
        virtualItemComp.BlockingEntity = blockingEnt;
        Dirty(virtualItem.Value, virtualItemComp);
        return true;
    }

    /// <summary>
    /// Queues a deletion for a virtual item and notifies the blocking entity and user.
    /// </summary>
    public void DeleteVirtualItem(Entity<VirtualItemComponent> item, EntityUid user)
    {
        var userEv = new VirtualItemDeletedEvent(item.Comp.BlockingEntity, user);
        RaiseLocalEvent(user, userEv);

        var targEv = new VirtualItemDeletedEvent(item.Comp.BlockingEntity, user);
        RaiseLocalEvent(item.Comp.BlockingEntity, targEv);

        if (TerminatingOrDeleted(item))
            return;

        _transformSystem.DetachParentToNull(item, Transform(item));
        if (_netManager.IsServer)
            QueueDel(item);
    }
}
