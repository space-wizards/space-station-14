using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Hands.EntitySystems;

public abstract partial class SharedHandsSystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] protected SharedContainerSystem ContainerSystem = default!;
    [Dependency] private SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedStorageSystem _storage = default!;
    [Dependency] protected SharedTransformSystem TransformSystem = default!;
    [Dependency] private SharedVirtualItemSystem _virtualSystem = default!;
    [Dependency] private EntityWhitelistSystem _entityWhitelist = default!;

    public event Action<Entity<HandsComponent>, string, HandLocation>? OnPlayerAddHand;
    public event Action<Entity<HandsComponent>, string>? OnPlayerRemoveHand;
    protected event Action<Entity<HandsComponent>?>? OnHandSetActive;

    public override void Initialize()
    {
        base.Initialize();

        InitializeInteractions();
        InitializeDrop();
        InitializePickup();
        InitializeRelay();
        InitializeEventListeners();

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
            var slot = ContainerSystem.EnsureContainer<ContainerSlot>(ent, id, container);
            slot.OccludesLight = false;
        }
    }

    private void OnMapInit(Entity<HandsComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.ActiveHandId == null)
            SetActiveHand(ent.AsNullable(), ent.Comp.SortedHands.FirstOrDefault());
    }

    /// <summary>
    /// Adds a hand with the given container id and supplied location to the specified entity.
    /// </summary>
    public void AddHand(Entity<HandsComponent?> ent, string handName, HandLocation handLocation, LocId? emptyLabel = null, EntProtoId? emptyRepresentative = null, EntityWhitelist? whitelist = null, EntityWhitelist? blacklist = null)
    {
        AddHand(ent, handName, new Hand(handLocation, emptyLabel, emptyRepresentative, whitelist, blacklist));
    }

    /// <summary>
    /// Adds a hand with the given container id and supplied hand definition to the given entity.
    /// </summary>
    public void AddHand(Entity<HandsComponent?> ent, string handName, Hand hand)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (ent.Comp.Hands.ContainsKey(handName))
            return;

        var container = ContainerSystem.EnsureContainer<ContainerSlot>(ent, handName);
        container.OccludesLight = false;

        ent.Comp.Hands.Add(handName, hand);
        ent.Comp.SortedHands.Add(handName);
        // we use LINQ + ToList instead of the list sort because it's a stable sort vs the list sort
        ent.Comp.SortedHands = ent.Comp.SortedHands.OrderBy(handId => ent.Comp.Hands[handId].Location).ToList();
        Dirty(ent);

        OnPlayerAddHand?.Invoke((ent, ent.Comp), handName, hand.Location);

        if (ent.Comp.ActiveHandId == null)
            SetActiveHand(ent, handName);

        RaiseLocalEvent(ent, new HandCountChangedEvent(ent));
    }

    /// <summary>
    /// Removes the specified hand from the specified entity
    /// </summary>
    public virtual void RemoveHand(Entity<HandsComponent?> ent, string handName)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        OnPlayerRemoveHand?.Invoke((ent, ent.Comp), handName);

        TryDrop(ent, handName, null, false);

        if (ContainerSystem.TryGetContainer(ent, handName, out var container))
            ContainerSystem.ShutdownContainer(container);

        if (!ent.Comp.Hands.Remove(handName))
            return;

        ent.Comp.SortedHands.Remove(handName);
        if (ent.Comp.ActiveHandId == handName)
            TrySetActiveHand(ent, ent.Comp.SortedHands.FirstOrDefault());

        RaiseLocalEvent(ent, new HandCountChangedEvent(ent));
        Dirty(ent);
    }

    /// <summary>
    /// Gets rid of all the entity's hands.
    /// </summary>
    public void RemoveHands(Entity<HandsComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var handIds = new List<string>(ent.Comp.Hands.Keys);
        foreach (var handId in handIds)
        {
            RemoveHand(ent, handId);
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
    public bool TryGetEmptyHand(Entity<HandsComponent?> ent, [NotNullWhen(true)] out string? emptyHand)
    {
        emptyHand = null;
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        foreach (var hand in EnumerateHands(ent))
        {
            if (HandIsEmpty(ent, hand))
            {
                emptyHand = hand;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Does this entity have any empty hands, and how many?
    /// </summary>
    public int GetEmptyHandCount(Entity<HandsComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false) || entity.Comp.Count == 0)
            return 0;

        var hands = 0;

        foreach (var hand in EnumerateHands(entity))
        {
            if (!HandIsEmpty(entity, hand))
                continue;
            hands++;
        }

        return hands;
    }

    /// <summary>
    /// Attempts to retrieve the item held in the entity's active hand.
    /// </summary>
    public bool TryGetActiveItem(Entity<HandsComponent?> entity, [NotNullWhen(true)] out EntityUid? item)
    {
        item = null;
        if (!Resolve(entity, ref entity.Comp, false))
            return false;

        if (!TryGetHeldItem(entity, entity.Comp.ActiveHandId, out var held))
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

    /// <summary>
    /// Gets the current active hand's Id for the specified entity
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public string? GetActiveHand(Entity<HandsComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return null;

        return entity.Comp.ActiveHandId;
    }

    /// <summary>
    /// Gets the current active hand's held entity for the specified entity
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public EntityUid? GetActiveItem(Entity<HandsComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return null;

        return GetHeldItem(entity, entity.Comp.ActiveHandId);
    }

    public bool ActiveHandIsEmpty(Entity<HandsComponent?> entity)
    {
        return GetActiveItem(entity) == null;
    }

    /// <summary>
    ///     Enumerate over hands, starting with the currently active hand.
    /// </summary>
    public HandEnumerable EnumerateHands(Entity<HandsComponent?> ent)
    {
        return new HandEnumerable(this, ent);
    }

    private HandEnumerator GetHandEnumerator(Entity<HandsComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return default;

        return new HandEnumerator(ent.Comp, ent.Comp.SortedHands.GetEnumerator());
    }

    public readonly struct HandEnumerable : IEnumerable<string>
    {
        private readonly SharedHandsSystem _handsSystem;
        private readonly Entity<HandsComponent?> _entity;

        internal HandEnumerable(SharedHandsSystem handsSystem, Entity<HandsComponent?> entity)
        {
            _handsSystem = handsSystem;
            _entity = entity;
        }

        public HandEnumerator GetEnumerator()
        {
            return _handsSystem.GetHandEnumerator(_entity);
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public struct HandEnumerator : IEnumerator<string>
    {
        private readonly HandsComponent _hands;
        private List<string>.Enumerator _handEnumerator;
        private bool _activeHandChecked;
        private bool _valid;

        internal HandEnumerator(HandsComponent hands, List<string>.Enumerator handEnumerator)
        {
            _hands = hands;
            _handEnumerator = handEnumerator;
            _activeHandChecked = false;
            _valid = true;
            Current = default!;
        }

        public string Current { get; private set; }

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (!_valid)
                return false;

            if (!_activeHandChecked)
            {
                _activeHandChecked = true;
                var activeHand = _hands.ActiveHandId;
                if (activeHand != null)
                {
                    Current = activeHand;
                    return true;
                }
            }

            while (_handEnumerator.MoveNext())
            {
                var hand = _handEnumerator.Current;
                if (hand == _hands.ActiveHandId)
                    continue;

                Current = hand;
                return true;
            }

            return false;
        }

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
            _handEnumerator.Dispose();
        }
    }

    /// <summary>
    ///     Enumerate over held items, starting with the item in the currently active hand (if there is one).
    /// </summary>
    public HeldEnumerable EnumerateHeld(Entity<HandsComponent?> ent)
    {
        return new HeldEnumerable(this, ent);
    }

    private HeldEnumerator GetHeldEnumerator(Entity<HandsComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return default;

        return new HeldEnumerator(this, ent, ent.Comp, ent.Comp.SortedHands.GetEnumerator());
    }

    public readonly struct HeldEnumerable : IEnumerable<EntityUid>
    {
        private readonly SharedHandsSystem _handsSystem;
        private readonly Entity<HandsComponent?> _entity;

        internal HeldEnumerable(SharedHandsSystem handsSystem, Entity<HandsComponent?> entity)
        {
            _handsSystem = handsSystem;
            _entity = entity;
        }

        public HeldEnumerator GetEnumerator()
        {
            return _handsSystem.GetHeldEnumerator(_entity);
        }

        IEnumerator<EntityUid> IEnumerable<EntityUid>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public struct HeldEnumerator : IEnumerator<EntityUid>
    {
        private readonly SharedHandsSystem _handsSystem;
        private readonly Entity<HandsComponent?> _entity;
        private readonly HandsComponent _hands;
        private List<string>.Enumerator _handEnumerator;
        private bool _activeHandChecked;
        private bool _valid;

        internal HeldEnumerator(SharedHandsSystem handsSystem, Entity<HandsComponent?> entity, HandsComponent hands, List<string>.Enumerator handEnumerator)
        {
            _handsSystem = handsSystem;
            _entity = entity;
            _hands = hands;
            _handEnumerator = handEnumerator;
            _activeHandChecked = false;
            _valid = true;
            Current = default;
        }

        public EntityUid Current { get; private set; }

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (!_valid)
                return false;

            if (!_activeHandChecked)
            {
                _activeHandChecked = true;
                if (_handsSystem.TryGetActiveItem(_entity, out var activeHeld))
                {
                    Current = activeHeld.Value;
                    return true;
                }
            }

            while (_handEnumerator.MoveNext())
            {
                var hand = _handEnumerator.Current;
                if (hand == _hands.ActiveHandId)
                    continue;

                if (_handsSystem.TryGetHeldItem(_entity, hand, out var held))
                {
                    Current = held.Value;
                    return true;
                }
            }

            return false;
        }

        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
            _handEnumerator.Dispose();
        }
    }

    /// <summary>
    ///     Set the currently active hand and raise hand (de)selection events directed at the held entities.
    /// </summary>
    /// <returns>True if the active hand was set to a NEW value. Setting it to the same value returns false and does
    /// not trigger interactions.</returns>
    public bool TrySetActiveHand(Entity<HandsComponent?> ent, string? name)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (name == ent.Comp.ActiveHandId)
            return false;

        if (name != null && !ent.Comp.Hands.ContainsKey(name))
            return false;
        return SetActiveHand(ent, name);
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

        if (TryGetActiveItem(ent, out var oldHeld))
            RaiseLocalEvent(oldHeld.Value, new HandDeselectedEvent(ent));

        if (handId == null)
        {
            ent.Comp.ActiveHandId = null;
            return true;
        }

        ent.Comp.ActiveHandId = handId;
        OnHandSetActive?.Invoke((ent, ent.Comp));

        if (TryGetHeldItem(ent, handId, out var newHeld))
            RaiseLocalEvent(newHeld.Value, new HandSelectedEvent(ent));

        Dirty(ent);
        return true;
    }

    /// <summary>
    ///     Cycles the currently active hand.
    /// </summary>
    public void SwapHands(Entity<HandsComponent?> ent, bool checkActionBlocker = false, bool reverse = false)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        if (checkActionBlocker && !_actionBlocker.CanInteract(ent.Owner, null))
            return;

        if (ent.Comp.ActiveHandId == null || ent.Comp.Hands.Count < 2)
            return;

        var currentIndex = ent.Comp.SortedHands.IndexOf(ent.Comp.ActiveHandId);
        var newActiveIndex = (currentIndex + (reverse ? -1 : 1) + ent.Comp.Hands.Count) % ent.Comp.Hands.Count;
        var nextHand = ent.Comp.SortedHands[newActiveIndex];

        TrySetActiveHand(ent, nextHand);
    }

    /// <summary>
    /// Checks if an item is being held by another entity.
    /// </summary>
    /// <param name="item">Item that we are checking.</param>
    /// <param name="user">User who is holding our item.</param>
    /// <returns>Returns true if our item is being held.</returns>
    public bool IsHeld(Entity<TransformComponent?> item, [NotNullWhen(true)] out EntityUid? user)
    {
        user = null;
        item.Comp ??= Transform(item);
        if (!IsHolding(item.Comp.ParentUid, item))
            return false;

        user = item.Comp.ParentUid;
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
            if (GetHeldItem(ent, hand) == entity)
            {
                inHand = hand;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to retrieve the associated hand struct corresponding to a hand ID on a given entity.
    /// </summary>
    public bool TryGetHand(Entity<HandsComponent?> ent, [NotNullWhen(true)] string? handId, [NotNullWhen(true)] out Hand? hand)
    {
        hand = null;

        if (handId == null)
            return false;

        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (!ent.Comp.Hands.TryGetValue(handId, out var handsHand))
            return false;

        hand = handsHand;
        return true;
    }

    /// <summary>
    /// Retrieves the item currently held in the specified hand of an entity.
    /// </summary>
    /// <param name="ent">The entity whose hands are being checked.</param>
    /// <param name="handId">The ID of the specific hand to check.</param>
    /// <param name="hideVirtualItems">If true, treats hands holding virtual items as if they are empty.</param>
    /// <returns>The UID of the held item, or null if the hand is empty, the hand ID is invalid, or the entity has no hands.</returns>
    public EntityUid? GetHeldItem(Entity<HandsComponent?> ent, string? handId, bool hideVirtualItems = false)
    {
        TryGetHeldItem(ent, handId, out var held, hideVirtualItems);
        return held;
    }

    /// <summary>
    /// Attempts to retrieve the item currently held in the specified hand of an entity.
    /// </summary>
    /// <param name="ent">The entity whose hands are being checked.</param>
    /// <param name="handId">The ID of the specific hand to check.</param>
    /// <param name="held">Outputs the UID of the held item, if one is found.</param>
    /// <param name="hideVirtualItems">If true, ignores virtual items and returns false if only a virtual item is present.</param>
    /// <returns>True if a valid item is found in the hand; otherwise, false.</returns>
    public bool TryGetHeldItem(Entity<HandsComponent?> ent, string? handId, [NotNullWhen(true)] out EntityUid? held, bool hideVirtualItems = false)
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

        if (hideVirtualItems && HasComp<VirtualItemComponent>(held))
            held = null;

        return held != null;
    }

    public bool HandIsEmpty(Entity<HandsComponent?> ent, string handId)
    {
        return GetHeldItem(ent, handId) == null;
    }

    /// <summary>
    /// Counts the number of hands on this entity.
    /// </summary>
    public int GetHandCount(Entity<HandsComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return 0;

        return ent.Comp.Hands.Count;
    }

    /// <summary>
    /// Counts the number of hands that are empty.
    /// </summary>
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

    /// <summary>
    /// Counts the number of hands that are empty or can be emptied by dropping an item.
    /// Unremoveable items will cause a hand to not be freeable.
    /// </summary>
    /// <param name="except">The hand this entity is in will be ignored when counting.</param>
    public int CountFreeableHands(Entity<HandsComponent> hands, EntityUid? except = null)
    {
        var freeable = 0;
        foreach (var name in hands.Comp.Hands.Keys)
        {
            if (except != null && GetHeldItem(hands.AsNullable(), name) == except)
                continue;

            if (HandIsEmpty(hands.AsNullable(), name) || CanDropHeld(hands, name))
                freeable++;
        }

        return freeable;
    }
}
