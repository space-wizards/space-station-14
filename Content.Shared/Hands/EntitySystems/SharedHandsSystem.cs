using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;

namespace Content.Shared.Hands.EntitySystems;

public abstract partial class SharedHandsSystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] protected readonly SharedContainerSystem ContainerSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interactionSystem = default!;
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
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<SharedHandsSystem>();
    }

    public virtual void AddHand(EntityUid uid, string handName, HandLocation handLocation, HandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            return;

        if (handsComp.Hands.ContainsKey(handName))
            return;

        var container = ContainerSystem.EnsureContainer<ContainerSlot>(uid, handName);
        container.OccludesLight = false;

        var newHand = new Hand(handName, handLocation, container);
        handsComp.Hands.Add(handName, newHand);
        handsComp.SortedHands.Add(handName);

        if (handsComp.ActiveHand == null)
            SetActiveHand(uid, newHand, handsComp);

        RaiseLocalEvent(uid, new HandCountChangedEvent(uid));
        Dirty(uid, handsComp);
    }

    public virtual void RemoveHand(EntityUid uid, string handName, HandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            return;

        if (!handsComp.Hands.Remove(handName, out var hand))
            return;

        handsComp.SortedHands.Remove(hand.Name);
        TryDrop(uid, hand, null, false, true, handsComp);
        if (hand.Container != null)
            ContainerSystem.ShutdownContainer(hand.Container);

        if (handsComp.ActiveHand == hand)
            TrySetActiveHand(uid, handsComp.SortedHands.FirstOrDefault(), handsComp);

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

        RemoveHands(uid, EnumerateHands(uid), handsComp);
    }

    private void RemoveHands(EntityUid uid, IEnumerable<Hand> hands, HandsComponent handsComp)
    {
        if (!hands.Any())
            return;

        var hand = hands.First();
        RemoveHand(uid, hand.Name, handsComp);

        // Repeats it for any additional hands.
        RemoveHands(uid, hands, handsComp);
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
    public bool TryGetEmptyHand(EntityUid uid, [NotNullWhen(true)] out Hand? emptyHand, HandsComponent? handComp = null)
    {
        emptyHand = null;
        if (!Resolve(uid, ref handComp, false))
            return false;

        foreach (var hand in EnumerateHands(uid, handComp))
        {
            if (hand.IsEmpty)
            {
                emptyHand = hand;
                return true;
            }
        }

        return false;
    }

    public bool TryGetActiveHand(Entity<HandsComponent?> entity, [NotNullWhen(true)] out Hand? hand)
    {
        if (!Resolve(entity, ref entity.Comp, false))
        {
            hand = null;
            return false;
        }

        hand = entity.Comp.ActiveHand;
        return hand != null;
    }

    public bool TryGetActiveItem(Entity<HandsComponent?> entity, [NotNullWhen(true)] out EntityUid? item)
    {
        if (!TryGetActiveHand(entity, out var hand))
        {
            item = null;
            return false;
        }

        item = hand.HeldEntity;
        return item != null;
    }

    public Hand? GetActiveHand(Entity<HandsComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return null;

        return entity.Comp.ActiveHand;
    }

    public EntityUid? GetActiveItem(Entity<HandsComponent?> entity)
    {
        return GetActiveHand(entity)?.HeldEntity;
    }

    /// <summary>
    ///     Enumerate over hands, starting with the currently active hand.
    /// </summary>
    public IEnumerable<Hand> EnumerateHands(EntityUid uid, HandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            yield break;

        if (handsComp.ActiveHand != null)
            yield return handsComp.ActiveHand;

        foreach (var name in handsComp.SortedHands)
        {
            if (name != handsComp.ActiveHand?.Name)
                yield return handsComp.Hands[name];
        }
    }

    /// <summary>
    ///     Enumerate over held items, starting with the item in the currently active hand (if there is one).
    /// </summary>
    public IEnumerable<EntityUid> EnumerateHeld(EntityUid uid, HandsComponent? handsComp = null)
    {
        if (!Resolve(uid, ref handsComp, false))
            yield break;

        if (handsComp.ActiveHandEntity != null)
            yield return handsComp.ActiveHandEntity.Value;

        foreach (var name in handsComp.SortedHands)
        {
            if (name == handsComp.ActiveHand?.Name)
                continue;

            if (handsComp.Hands[name].HeldEntity is { } held)
                yield return held;
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

        if (name == handComp.ActiveHand?.Name)
            return false;

        Hand? hand = null;
        if (name != null && !handComp.Hands.TryGetValue(name, out hand))
            return false;
        return SetActiveHand(uid, hand, handComp);
    }

    /// <summary>
    ///     Set the currently active hand and raise hand (de)selection events directed at the held entities.
    /// </summary>
    /// <returns>True if the active hand was set to a NEW value. Setting it to the same value returns false and does
    /// not trigger interactions.</returns>
    public bool SetActiveHand(EntityUid uid, Hand? hand, HandsComponent? handComp = null)
    {
        if (!Resolve(uid, ref handComp))
            return false;

        if (hand == handComp.ActiveHand)
            return false;

        if (handComp.ActiveHand?.HeldEntity is { } held)
            RaiseLocalEvent(held, new HandDeselectedEvent(uid));

        if (hand == null)
        {
            handComp.ActiveHand = null;
            return true;
        }

        handComp.ActiveHand = hand;
        OnHandSetActive?.Invoke((uid, handComp));

        if (hand.HeldEntity != null)
            RaiseLocalEvent(hand.HeldEntity.Value, new HandSelectedEvent(uid));

        Dirty(uid, handComp);
        return true;
    }

    public bool IsHolding(Entity<HandsComponent?> entity, [NotNullWhen(true)] EntityUid? item)
    {
        return IsHolding(entity, item, out _, entity);
    }

    public bool IsHolding(EntityUid uid, [NotNullWhen(true)] EntityUid? entity, [NotNullWhen(true)] out Hand? inHand, HandsComponent? handsComp = null)
    {
        inHand = null;
        if (entity == null)
            return false;

        if (!Resolve(uid, ref handsComp, false))
            return false;

        foreach (var hand in handsComp.Hands.Values)
        {
            if (hand.HeldEntity == entity)
            {
                inHand = hand;
                return true;
            }
        }

        return false;
    }

    public bool TryGetHand(EntityUid handsUid, string handId, [NotNullWhen(true)] out Hand? hand,
        HandsComponent? hands = null)
    {
        hand = null;

        if (!Resolve(handsUid, ref hands))
            return false;

        return hands.Hands.TryGetValue(handId, out hand);
    }

    public int CountFreeableHands(Entity<HandsComponent> hands)
    {
        var freeable = 0;
        foreach (var hand in hands.Comp.Hands.Values)
        {
            if (hand.IsEmpty || CanDropHeld(hands, hand))
                freeable++;
        }

        return freeable;
    }
}
