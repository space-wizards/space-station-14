using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Utility;

namespace Content.Shared.Hands.EntitySystems;

public abstract partial class SharedHandsSystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] protected readonly SharedContainerSystem ContainerSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualSystem = default!;

    protected event Action<Entity<HandsComponent>?>? OnHandSetActive;

    public override void Initialize()
    {
        base.Initialize();

        InitializeInteractions();
        InitializeDrop();
        InitializePickup();
        InitializeRelay();

        SubscribeLocalEvent<HandsComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<HandsComponent, MapInitEvent>(OnMapInit);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<SharedHandsSystem>();
    }

    private void OnInit(Entity<HandsComponent> ent, ref ComponentInit args)
    {
        var container = EnsureComp<ContainerManagerComponent>(ent);
        foreach (var id in ent.Comp.Hands.Keys)
        {
            ContainerSystem.EnsureContainer<ContainerSlot>(ent, id, container);
        }
    }

    private void OnMapInit(Entity<HandsComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.ActiveHandId == null)
            SetActiveHand(ent.AsNullable(), ent.Comp.SortedHands.FirstOrDefault());
    }

    public void AddHand(EntityUid uid, string handName, HandLocation handLocation, HandsComponent? handsComp = null)
    {
        AddHand(uid, handName, new Hand(handLocation), handsComp);
    }

    public virtual void AddHand(EntityUid uid, string handName, Hand hand, HandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            return;

        if (handsComp.Hands.ContainsKey(handName))
            return;

        var container = ContainerSystem.EnsureContainer<ContainerSlot>(uid, handName);
        container.OccludesLight = false;

        handsComp.Hands.Add(handName, hand);
        handsComp.SortedHands.Add(handName);

        if (handsComp.ActiveHandId == null)
            SetActiveHand((uid, handsComp), handName);

        RaiseLocalEvent(uid, new HandCountChangedEvent(uid));
        Dirty(uid, handsComp);
    }

    public virtual void RemoveHand(EntityUid uid, string handName, HandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            return;

        if (!handsComp.Hands.Remove(handName))
            return;

        TryDrop(uid, handName, null, false, true, handsComp);
        if (ContainerSystem.TryGetContainer(uid, handName, out var container))
            ContainerSystem.ShutdownContainer(container);

        if (handsComp.ActiveHandId == handName)
            TrySetActiveHand(uid, handsComp.SortedHands.FirstOrDefault(), handsComp);

        handsComp.SortedHands.Remove(handName);
        RaiseLocalEvent(uid, new HandCountChangedEvent(uid));
        Dirty(uid, handsComp);
    }

    /// <summary>
    /// Gets rid of all the entity's hands.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="handsComp"></param>
    public void RemoveHands(EntityUid uid, HandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp))
            return;

        var handIds = new List<string>(handsComp.Hands.Keys);
        foreach (var handId in handIds)
        {
            RemoveHand(uid, handId, handsComp);
        }
    }

    private void HandleSetHand(RequestSetHandEvent msg, EntitySessionEventArgs eventArgs)
    {
        if (eventArgs.SenderSession.AttachedEntity == null)
            return;

        TrySetActiveHand(eventArgs.SenderSession.AttachedEntity.Value, msg.HandName);
    }

    /// <summary>
    ///     Get any empty hand. Prioritizes the currently active hand.
    /// </summary>
    public bool TryGetEmptyHand(EntityUid uid, [NotNullWhen(true)] out string? emptyHand, HandsComponent? handComp = null)
    {
        emptyHand = null;
        if (!Resolve(uid, ref handComp, false))
            return false;

        foreach (var hand in EnumerateHands((uid, handComp)))
        {
            if (HandIsEmpty((uid, handComp), hand))
            {
                emptyHand = hand;
                return true;
            }
        }

        return false;
    }

    public bool TryGetActiveItem(Entity<HandsComponent?> entity, [NotNullWhen(true)] out EntityUid? item)
    {
        item = null;
        if (!Resolve(entity, ref entity.Comp, false))
            return false;

        if (!TryGetHeldEntity(entity, entity.Comp.ActiveHandId, out var held))
            return false;

        item = held;
        return true;
    }

    /// <summary>
    /// Gets active hand item if relevant otherwise gets the entity itself.
    /// </summary>
    public EntityUid GetActiveItemOrSelf(Entity<HandsComponent?> entity)
    {
        if (!TryGetActiveItem(entity, out var item))
        {
            return entity.Owner;
        }

        return item.Value;
    }

    public string? GetActiveHand(Entity<HandsComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return null;

        return entity.Comp.ActiveHandId;
    }

    public EntityUid? GetActiveItem(Entity<HandsComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return null;

        return GetHeldEntityOrNull(entity, entity.Comp.ActiveHandId);
    }

    public bool ActiveHandIsEmpty(Entity<HandsComponent?> entity)
    {
        return GetActiveItem(entity) == null;
    }

    /// <summary>
    ///     Enumerate over hands, starting with the currently active hand.
    /// </summary>
    public IEnumerable<string> EnumerateHands(Entity<HandsComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            yield break;

        if (ent.Comp.ActiveHandId != null)
            yield return ent.Comp.ActiveHandId;

        foreach (var name in ent.Comp.SortedHands)
        {
            if (name != ent.Comp.ActiveHandId)
                yield return name;
        }
    }

    /// <summary>
    ///     Enumerate over held items, starting with the item in the currently active hand (if there is one).
    /// </summary>
    public IEnumerable<EntityUid> EnumerateHeld(EntityUid uid, HandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            yield break;

        if (TryGetActiveItem((uid, handsComp), out var activeHeld))
            yield return activeHeld.Value;

        foreach (var name in handsComp.SortedHands)
        {
            if (name == handsComp.ActiveHandId)
                continue;

            if (TryGetHeldEntity((uid, handsComp), name, out var held))
                yield return held.Value;
        }
    }

    /// <summary>
    ///     Set the currently active hand and raise hand (de)selection events directed at the held entities.
    /// </summary>
    /// <returns>True if the active hand was set to a NEW value. Setting it to the same value returns false and does
    /// not trigger interactions.</returns>
    public virtual bool TrySetActiveHand(EntityUid uid, string? name, HandsComponent? handComp = null)
    {
        if (!Resolve(uid, ref handComp))
            return false;

        if (name == handComp.ActiveHandId)
            return false;

        if (name != null && !handComp.Hands.ContainsKey(name))
            return false;
        return SetActiveHand((uid, handComp), name);
    }

    /// <summary>
    ///     Set the currently active hand and raise hand (de)selection events directed at the held entities.
    /// </summary>
    /// <returns>True if the active hand was set to a NEW value. Setting it to the same value returns false and does
    /// not trigger interactions.</returns>
    public bool SetActiveHand(Entity<HandsComponent?> ent, string? handId)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (handId == ent.Comp.ActiveHandId)
            return false;

        if (TryGetHeldEntity(ent, handId, out var oldHeld))
            RaiseLocalEvent(oldHeld.Value, new HandDeselectedEvent(ent));

        if (handId == null)
        {
            ent.Comp.ActiveHandId = null;
            return true;
        }

        ent.Comp.ActiveHandId = handId;
        OnHandSetActive?.Invoke((ent, ent.Comp));

        if (TryGetHeldEntity(ent, handId, out var newHeld))
            RaiseLocalEvent(newHeld.Value, new HandSelectedEvent(ent));

        Dirty(ent);
        return true;
    }

    public bool IsHolding(Entity<HandsComponent?> entity, [NotNullWhen(true)] EntityUid? item)
    {
        return IsHolding(entity, item, out _);
    }

    public bool IsHolding(Entity<HandsComponent?> ent, [NotNullWhen(true)] EntityUid? entity, [NotNullWhen(true)] out string? inHand)
    {
        inHand = null;
        if (entity == null)
            return false;

        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        foreach (var hand in ent.Comp.Hands.Keys)
        {
            if (GetHeldEntityOrNull(ent, hand) == entity)
            {
                inHand = hand;
                return true;
            }
        }

        return false;
    }

    public bool TryGetHand(EntityUid handsUid, [NotNullWhen(true)] string? handId, [NotNullWhen(true)] out Hand? hand, HandsComponent? hands = null)
    {
        hand = null;

        if (handId == null)
            return false;

        if (!Resolve(handsUid, ref hands))
            return false;

        return hands.Hands.TryGetValue(handId, out hand);
    }

    public EntityUid? GetHeldEntityOrNull(Entity<HandsComponent?> ent, string? handId)
    {
        TryGetHeldEntity(ent, handId, out var held);
        return held;
    }

    public bool TryGetHeldEntity(Entity<HandsComponent?> ent, string? handId, [NotNullWhen(true)] out EntityUid? held)
    {
        held = null;
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        // Sanity check to make sure this is actually a hand.
        if (handId == null || !ent.Comp.Hands.ContainsKey(handId))
            return false;

        if (!ContainerSystem.TryGetContainer(ent, handId, out var container))
            return false;

        held = container.ContainedEntities.FirstOrNull();
        return held != null;
    }

    public bool HandIsEmpty(Entity<HandsComponent?> ent, string handId)
    {
        return GetHeldEntityOrNull(ent, handId) == null;
    }

    public int GetHandCount(Entity<HandsComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return 0;

        return ent.Comp.Hands.Count;
    }

    public int CountFreeHands(Entity<HandsComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return 0;

        var free = 0;
        foreach (var name in ent.Comp.Hands.Keys)
        {
            if (HandIsEmpty(ent, name))
                free++;
        }

        return free;
    }

    public int CountFreeableHands(Entity<HandsComponent> hands)
    {
        var freeable = 0;
        foreach (var name in hands.Comp.Hands.Keys)
        {
            if (HandIsEmpty(hands.AsNullable(), name) || CanDropHeld(hands, name))
                freeable++;
        }

        return freeable;
    }
}
