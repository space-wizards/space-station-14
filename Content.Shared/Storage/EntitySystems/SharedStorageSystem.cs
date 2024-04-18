using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Coordinates;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Lock;
using Content.Shared.Materials;
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Storage.Components;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.Storage.EntitySystems;

public abstract class SharedStorageSystem : EntitySystem
{
    [Dependency] private   readonly IPrototypeManager _prototype = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] protected readonly ActionBlockerSystem ActionBlocker = default!;
    [Dependency] private   readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private   readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] private   readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private   readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] protected readonly SharedEntityStorageSystem EntityStorage = default!;
    [Dependency] private   readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] protected readonly SharedItemSystem ItemSystem = default!;
    [Dependency] private   readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private   readonly SharedHandsSystem _sharedHandsSystem = default!;
    [Dependency] private   readonly SharedStackSystem _stack = default!;
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;
    [Dependency] private   readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] protected readonly UseDelaySystem UseDelay = default!;

    private EntityQuery<ItemComponent> _itemQuery;
    private EntityQuery<StackComponent> _stackQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    [ValidatePrototypeId<ItemSizePrototype>]
    public const string DefaultStorageMaxItemSize = "Normal";

    public const float AreaInsertDelayPerItem = 0.075f;

    private ItemSizePrototype _defaultStorageMaxItemSize = default!;

    public bool CheckingCanInsert;

    private readonly List<ItemSizePrototype> _sortedSizes = new();
    private FrozenDictionary<string, ItemSizePrototype> _nextSmallest = FrozenDictionary<string, ItemSizePrototype>.Empty;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        _itemQuery = GetEntityQuery<ItemComponent>();
        _stackQuery = GetEntityQuery<StackComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();
        _prototype.PrototypesReloaded += OnPrototypesReloaded;

        SubscribeLocalEvent<StorageComponent, ComponentGetState>(OnStorageGetState);
        SubscribeLocalEvent<StorageComponent, ComponentHandleState>(OnStorageHandleState);
        SubscribeLocalEvent<StorageComponent, ComponentInit>(OnComponentInit, before: new[] { typeof(SharedContainerSystem) });
        SubscribeLocalEvent<StorageComponent, GetVerbsEvent<UtilityVerb>>(AddTransferVerbs);
        SubscribeLocalEvent<StorageComponent, InteractUsingEvent>(OnInteractUsing, after: new[] { typeof(ItemSlotsSystem) });
        SubscribeLocalEvent<StorageComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<StorageComponent, OpenStorageImplantEvent>(OnImplantActivate);
        SubscribeLocalEvent<StorageComponent, AfterInteractEvent>(AfterInteract);
        SubscribeLocalEvent<StorageComponent, DestructionEventArgs>(OnDestroy);
        SubscribeLocalEvent<StorageComponent, BoundUIOpenedEvent>(OnBoundUIOpen);
        SubscribeLocalEvent<MetaDataComponent, StackCountChangedEvent>(OnStackCountChanged);

        SubscribeLocalEvent<StorageComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<StorageComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<StorageComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);

        SubscribeLocalEvent<StorageComponent, AreaPickupDoAfterEvent>(OnDoAfter);

        SubscribeAllEvent<StorageInteractWithItemEvent>(OnInteractWithItem);
        SubscribeAllEvent<StorageSetItemLocationEvent>(OnSetItemLocation);
        SubscribeAllEvent<StorageInsertItemIntoLocationEvent>(OnInsertItemIntoLocation);
        SubscribeAllEvent<StorageRemoveItemEvent>(OnRemoveItem);
        SubscribeAllEvent<StorageSaveItemLocationEvent>(OnSaveItemLocation);

        SubscribeLocalEvent<StorageComponent, GotReclaimedEvent>(OnReclaimed);

        UpdatePrototypeCache();
    }

    private void OnStorageGetState(EntityUid uid, StorageComponent component, ref ComponentGetState args)
    {
        var storedItems = new Dictionary<NetEntity, ItemStorageLocation>();

        foreach (var (ent, location) in component.StoredItems)
        {
            storedItems[GetNetEntity(ent)] = location;
        }

        args.State = new StorageComponentState()
        {
            Grid = new List<Box2i>(component.Grid),
            IsUiOpen = component.IsUiOpen,
            MaxItemSize = component.MaxItemSize,
            StoredItems = storedItems,
            SavedLocations = component.SavedLocations
        };
    }

    private void OnStorageHandleState(EntityUid uid, StorageComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not StorageComponentState state)
            return;

        component.Grid.Clear();
        component.Grid.AddRange(state.Grid);
        component.IsUiOpen = state.IsUiOpen;
        component.MaxItemSize = state.MaxItemSize;

        component.StoredItems.Clear();

        foreach (var (nent, location) in state.StoredItems)
        {
            var ent = EnsureEntity<StorageComponent>(nent, uid);
            component.StoredItems[ent] = location;
        }

        component.SavedLocations = state.SavedLocations;
    }

    public override void Shutdown()
    {
        _prototype.PrototypesReloaded -= OnPrototypesReloaded;
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.ByType.ContainsKey(typeof(ItemSizePrototype))
            || (args.Removed?.ContainsKey(typeof(ItemSizePrototype)) ?? false))
        {
            UpdatePrototypeCache();
        }
    }

    private void UpdatePrototypeCache()
    {
        _defaultStorageMaxItemSize = _prototype.Index<ItemSizePrototype>(DefaultStorageMaxItemSize);
        _sortedSizes.Clear();
        _sortedSizes.AddRange(_prototype.EnumeratePrototypes<ItemSizePrototype>());
        _sortedSizes.Sort();

        var nextSmallest = new KeyValuePair<string, ItemSizePrototype>[_sortedSizes.Count];
        for (var i = 0; i < _sortedSizes.Count; i++)
        {
            var k = _sortedSizes[i].ID;
            var v = _sortedSizes[Math.Max(i - 1, 0)];
            nextSmallest[i] = new(k, v);
        }

        _nextSmallest = nextSmallest.ToFrozenDictionary();
    }

    private void OnComponentInit(EntityUid uid, StorageComponent storageComp, ComponentInit args)
    {
        storageComp.Container = _containerSystem.EnsureContainer<Container>(uid, StorageComponent.ContainerId);
        UpdateAppearance((uid, storageComp, null));
    }

    public virtual void UpdateUI(Entity<StorageComponent?> entity) {}

    public virtual void OpenStorageUI(EntityUid uid, EntityUid entity, StorageComponent? storageComp = null, bool silent = false) { }

    private void AddTransferVerbs(EntityUid uid, StorageComponent component, GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var entities = component.Container.ContainedEntities;

        if (entities.Count == 0 || TryComp(uid, out LockComponent? lockComponent) && lockComponent.Locked)
            return;

        // if the target is storage, add a verb to transfer storage.
        if (TryComp(args.Target, out StorageComponent? targetStorage)
            && (!TryComp(args.Target, out LockComponent? targetLock) || !targetLock.Locked))
        {
            UtilityVerb verb = new()
            {
                Text = Loc.GetString("storage-component-transfer-verb"),
                IconEntity = GetNetEntity(args.Using),
                Act = () => TransferEntities(uid, args.Target, args.User, component, lockComponent, targetStorage, targetLock)
            };

            args.Verbs.Add(verb);
        }
    }

    /// <summary>
    /// Inserts storable entities into this storage container if possible, otherwise return to the hand of the user
    /// </summary>
    /// <returns>true if inserted, false otherwise</returns>
    private void OnInteractUsing(EntityUid uid, StorageComponent storageComp, InteractUsingEvent args)
    {
        if (args.Handled || !storageComp.ClickInsert || TryComp(uid, out LockComponent? lockComponent) && lockComponent.Locked)
            return;

        if (HasComp<PlaceableSurfaceComponent>(uid))
            return;

        PlayerInsertHeldEntity(uid, args.User, storageComp);
        // Always handle it, even if insertion fails.
        // We don't want to trigger any AfterInteract logic here.
        // Example issue would be placing wires if item doesn't fit in backpack.
        args.Handled = true;
    }

    /// <summary>
    /// Sends a message to open the storage UI
    /// </summary>
    private void OnActivate(EntityUid uid, StorageComponent storageComp, ActivateInWorldEvent args)
    {
        if (args.Handled || TryComp<LockComponent>(uid, out var lockComponent) && lockComponent.Locked)
            return;

        OpenStorageUI(uid, args.User, storageComp);
        args.Handled = true;
    }

    /// <summary>
    /// Specifically for storage implants.
    /// </summary>
    private void OnImplantActivate(EntityUid uid, StorageComponent storageComp, OpenStorageImplantEvent args)
    {
        if (args.Handled)
            return;

        OpenStorageUI(uid, args.Performer, storageComp);
        args.Handled = true;
    }

    /// <summary>
    /// Allows a user to pick up entities by clicking them, or pick up all entities in a certain radius
    /// around a click.
    /// </summary>
    /// <returns></returns>
    private void AfterInteract(EntityUid uid, StorageComponent storageComp, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        // Pick up all entities in a radius around the clicked location.
        // The last half of the if is because carpets exist and this is terrible
        if (storageComp.AreaInsert && (args.Target == null || !HasComp<ItemComponent>(args.Target.Value)))
        {
            var validStorables = new List<EntityUid>();
            var delay = 0f;

            foreach (var entity in _entityLookupSystem.GetEntitiesInRange(args.ClickLocation, storageComp.AreaInsertRadius, LookupFlags.Dynamic | LookupFlags.Sundries))
            {
                if (entity == args.User
                    // || !_itemQuery.HasComponent(entity)
                    || !TryComp<ItemComponent>(entity, out var itemComp) // Need comp to get item size to get weight
                    || !_prototype.TryIndex(itemComp.Size, out var itemSize)
                    || !CanInsert(uid, entity, out _, storageComp)
                    || !_interactionSystem.InRangeUnobstructed(args.User, entity))
                {
                    continue;
                }

                validStorables.Add(entity);
                delay += itemSize.Weight * AreaInsertDelayPerItem;
            }

            //If there's only one then let's be generous
            if (validStorables.Count > 1)
            {
                var doAfterArgs = new DoAfterArgs(EntityManager, args.User, delay, new AreaPickupDoAfterEvent(GetNetEntityList(validStorables)), uid, target: uid)
                {
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    NeedHand = true
                };

                _doAfterSystem.TryStartDoAfter(doAfterArgs);
                args.Handled = true;
            }

            return;
        }

        // Pick up the clicked entity
        if (storageComp.QuickInsert)
        {
            if (args.Target is not { Valid: true } target)
                return;

            if (_containerSystem.IsEntityInContainer(target)
                || target == args.User
                || !HasComp<ItemComponent>(target))
            {
                return;
            }

            if (_xformQuery.TryGetComponent(uid, out var transformOwner) && TryComp<TransformComponent>(target, out var transformEnt))
            {
                var parent = transformOwner.ParentUid;

                var position = EntityCoordinates.FromMap(
                    parent.IsValid() ? parent : uid,
                    TransformSystem.GetMapCoordinates(transformEnt),
                    TransformSystem
                );

                args.Handled = true;
                if (PlayerInsertEntityInWorld((uid, storageComp), args.User, target))
                {
                    RaiseNetworkEvent(new AnimateInsertingEntitiesEvent(GetNetEntity(uid),
                        new List<NetEntity> { GetNetEntity(target) },
                        new List<NetCoordinates> { GetNetCoordinates(position) },
                        new List<Angle> { transformOwner.LocalRotation }));
                }
            }
        }
    }

    private void OnDoAfter(EntityUid uid, StorageComponent component, AreaPickupDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;
        var successfullyInserted = new List<EntityUid>();
        var successfullyInsertedPositions = new List<EntityCoordinates>();
        var successfullyInsertedAngles = new List<Angle>();
        _xformQuery.TryGetComponent(uid, out var xform);

        foreach (var netEntity in args.Entities)
        {
            var entity = GetEntity(netEntity);

            // Check again, situation may have changed for some entities, but we'll still pick up any that are valid
            if (_containerSystem.IsEntityInContainer(entity)
                || entity == args.Args.User
                || !_itemQuery.HasComponent(entity))
                continue;

            if (xform == null ||
                !_xformQuery.TryGetComponent(entity, out var targetXform) ||
                targetXform.MapID != xform.MapID)
            {
                continue;
            }

            var position = EntityCoordinates.FromMap(
                xform.ParentUid.IsValid() ? xform.ParentUid : uid,
                new MapCoordinates(TransformSystem.GetWorldPosition(targetXform), targetXform.MapID),
                TransformSystem
            );

            var angle = targetXform.LocalRotation;

            if (PlayerInsertEntityInWorld((uid, component), args.Args.User, entity))
            {
                successfullyInserted.Add(entity);
                successfullyInsertedPositions.Add(position);
                successfullyInsertedAngles.Add(angle);
            }
        }

        // If we picked up at least one thing, play a sound and do a cool animation!
        if (successfullyInserted.Count > 0)
        {
            Audio.PlayPvs(component.StorageInsertSound, uid);
            RaiseNetworkEvent(new AnimateInsertingEntitiesEvent(
                GetNetEntity(uid),
                GetNetEntityList(successfullyInserted),
                GetNetCoordinatesList(successfullyInsertedPositions),
                successfullyInsertedAngles));
        }

        args.Handled = true;
    }

    private void OnReclaimed(EntityUid uid, StorageComponent storageComp, GotReclaimedEvent args)
    {
        _containerSystem.EmptyContainer(storageComp.Container, destination: args.ReclaimerCoordinates);
    }

    private void OnDestroy(EntityUid uid, StorageComponent storageComp, DestructionEventArgs args)
    {
        var coordinates = TransformSystem.GetMoverCoordinates(uid);

        // Being destroyed so need to recalculate.
        _containerSystem.EmptyContainer(storageComp.Container, destination: coordinates);
    }

    /// <summary>
    ///     This function gets called when the user clicked on an item in the storage UI. This will either place the
    ///     item in the user's hand if it is currently empty, or interact with the item using the user's currently
    ///     held item.
    /// </summary>
    private void OnInteractWithItem(StorageInteractWithItemEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } player)
            return;

        var uid = GetEntity(msg.StorageUid);
        var entity = GetEntity(msg.InteractedItemUid);

        if (!TryComp<StorageComponent>(uid, out var storageComp))
            return;

        if (!_ui.TryGetUi(uid, StorageComponent.StorageUiKey.Key, out var bui) ||
            !bui.SubscribedSessions.Contains(args.SenderSession))
            return;

        if (!Exists(entity))
        {
            Log.Error($"Player {args.SenderSession} interacted with non-existent item {msg.InteractedItemUid} stored in {ToPrettyString(uid)}");
            return;
        }

        if (!ActionBlocker.CanInteract(player, entity) || !storageComp.Container.Contains(entity))
            return;

        // Does the player have hands?
        if (!TryComp(player, out HandsComponent? hands) || hands.Count == 0)
            return;

        // If the user's active hand is empty, try pick up the item.
        if (hands.ActiveHandEntity == null)
        {
            if (_sharedHandsSystem.TryPickupAnyHand(player, entity, handsComp: hands)
                && storageComp.StorageRemoveSound != null)
                Audio.PlayPredicted(storageComp.StorageRemoveSound, uid, player);
            {
                return;
            }
        }

        // Else, interact using the held item
        _interactionSystem.InteractUsing(player, hands.ActiveHandEntity.Value, entity, Transform(entity).Coordinates, checkCanInteract: false);
    }

    private void OnSetItemLocation(StorageSetItemLocationEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } player)
            return;

        var storageEnt = GetEntity(msg.StorageEnt);
        var itemEnt = GetEntity(msg.ItemEnt);

        if (!TryComp<StorageComponent>(storageEnt, out var storageComp))
            return;

        if (!_ui.TryGetUi(storageEnt, StorageComponent.StorageUiKey.Key, out var bui) ||
            !bui.SubscribedSessions.Contains(args.SenderSession))
            return;

        if (!Exists(itemEnt))
        {
            Log.Error($"Player {args.SenderSession} set location of non-existent item {msg.ItemEnt} stored in {ToPrettyString(storageEnt)}");
            return;
        }

        if (!ActionBlocker.CanInteract(player, itemEnt))
            return;

        TrySetItemStorageLocation((itemEnt, null), (storageEnt, storageComp), msg.Location);
    }

    private void OnRemoveItem(StorageRemoveItemEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } player)
            return;

        var storageEnt = GetEntity(msg.StorageEnt);
        var itemEnt = GetEntity(msg.ItemEnt);

        if (!TryComp<StorageComponent>(storageEnt, out var storageComp))
            return;

        if (!_ui.TryGetUi(storageEnt, StorageComponent.StorageUiKey.Key, out var bui) ||
            !bui.SubscribedSessions.Contains(args.SenderSession))
            return;

        if (!Exists(itemEnt))
        {
            Log.Error($"Player {args.SenderSession} set location of non-existent item {msg.ItemEnt} stored in {ToPrettyString(storageEnt)}");
            return;
        }

        if (!ActionBlocker.CanInteract(player, itemEnt))
            return;

        TransformSystem.DropNextTo(itemEnt, player);
        Audio.PlayPredicted(storageComp.StorageRemoveSound, storageEnt, player);
    }

    private void OnInsertItemIntoLocation(StorageInsertItemIntoLocationEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } player)
            return;

        var storageEnt = GetEntity(msg.StorageEnt);
        var itemEnt = GetEntity(msg.ItemEnt);

        if (!TryComp<StorageComponent>(storageEnt, out var storageComp))
            return;

        if (!_ui.TryGetUi(storageEnt, StorageComponent.StorageUiKey.Key, out var bui) ||
            !bui.SubscribedSessions.Contains(args.SenderSession))
            return;

        if (!Exists(itemEnt))
        {
            Log.Error($"Player {args.SenderSession} set location of non-existent item {msg.ItemEnt} stored in {ToPrettyString(storageEnt)}");
            return;
        }

        if (!ActionBlocker.CanInteract(player, itemEnt) || !_sharedHandsSystem.IsHolding(player, itemEnt, out _))
            return;

        InsertAt((storageEnt, storageComp), (itemEnt, null), msg.Location, out _, player, stackAutomatically: false);
    }

    // TODO: if/when someone cleans up this shitcode please make all these
    // handlers use a shared helper for checking that the ui is open etc, thanks
    private void OnSaveItemLocation(StorageSaveItemLocationEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not {} player)
            return;

        var storage = GetEntity(msg.Storage);
        var item = GetEntity(msg.Item);

        if (!TryComp<StorageComponent>(storage, out var storageComp))
            return;

        if (!_ui.TryGetUi(storage, StorageComponent.StorageUiKey.Key, out var bui) ||
            !bui.SubscribedSessions.Contains(args.SenderSession))
            return;

        if (!Exists(item))
        {
            Log.Error($"Player {args.SenderSession} saved location of non-existent item {msg.Item} stored in {ToPrettyString(storage)}");
            return;
        }

        if (!ActionBlocker.CanInteract(player, item))
            return;

        SaveItemLocation(storage, item);
    }

    private void OnBoundUIOpen(EntityUid uid, StorageComponent storageComp, BoundUIOpenedEvent args)
    {
        if (!storageComp.IsUiOpen)
        {
            storageComp.IsUiOpen = true;
            UpdateAppearance((uid, storageComp, null));
        }
    }

    private void OnEntInserted(Entity<StorageComponent> entity, ref EntInsertedIntoContainerMessage args)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (entity.Comp.Container == null)
            return;

        if (args.Container.ID != StorageComponent.ContainerId)
            return;

        if (!entity.Comp.StoredItems.ContainsKey(args.Entity))
        {
            if (!TryGetAvailableGridSpace((entity.Owner, entity.Comp), (args.Entity, null), out var location))
            {
                _containerSystem.Remove(args.Entity, args.Container, force: true);
                return;
            }

            entity.Comp.StoredItems[args.Entity] = location.Value;
            Dirty(entity, entity.Comp);
        }

        UpdateAppearance((entity, entity.Comp, null));
        UpdateUI((entity, entity.Comp));
    }

    private void OnEntRemoved(Entity<StorageComponent> entity, ref EntRemovedFromContainerMessage args)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (entity.Comp.Container == null)
            return;

        if (args.Container.ID != StorageComponent.ContainerId)
            return;

        entity.Comp.StoredItems.Remove(args.Entity);
        Dirty(entity, entity.Comp);

        UpdateAppearance((entity, entity.Comp, null));
        UpdateUI((entity, entity.Comp));
    }

    private void OnInsertAttempt(EntityUid uid, StorageComponent component, ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled || args.Container.ID != StorageComponent.ContainerId)
            return;

        // don't run cyclical CanInsert() loops
        if (CheckingCanInsert)
            return;

        if (!CanInsert(uid, args.EntityUid, out _, component, ignoreStacks: true))
            args.Cancel();
    }

    public void UpdateAppearance(Entity<StorageComponent?, AppearanceComponent?> entity)
    {
        // TODO STORAGE remove appearance data and just use the data on the component.
        var (uid, storage, appearance) = entity;
        if (!Resolve(uid, ref storage, ref appearance, false))
            return;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (storage.Container == null)
            return; // component hasn't yet been initialized.

        var capacity = storage.Grid.GetArea();
        var used = GetCumulativeItemAreas((uid, storage));

        _appearance.SetData(uid, StorageVisuals.StorageUsed, used, appearance);
        _appearance.SetData(uid, StorageVisuals.Capacity, capacity, appearance);
        _appearance.SetData(uid, StorageVisuals.Open, storage.IsUiOpen, appearance);
        _appearance.SetData(uid, SharedBagOpenVisuals.BagState, storage.IsUiOpen ? SharedBagState.Open : SharedBagState.Closed, appearance);
        _appearance.SetData(uid, StackVisuals.Hide, !storage.IsUiOpen, appearance);
    }

    /// <summary>
    ///     Move entities from one storage to another.
    /// </summary>
    public void TransferEntities(EntityUid source, EntityUid target, EntityUid? user = null,
        StorageComponent? sourceComp = null, LockComponent? sourceLock = null,
        StorageComponent? targetComp = null, LockComponent? targetLock = null)
    {
        if (!Resolve(source, ref sourceComp) || !Resolve(target, ref targetComp))
            return;

        var entities = sourceComp.Container.ContainedEntities;
        if (entities.Count == 0)
            return;

        if (Resolve(source, ref sourceLock, false) && sourceLock.Locked
            || Resolve(target, ref targetLock, false) && targetLock.Locked)
            return;

        foreach (var entity in entities.ToArray())
        {
            Insert(target, entity, out _, user: user, targetComp, playSound: false);
        }

        Audio.PlayPredicted(sourceComp.StorageInsertSound, target, user);
    }

    /// <summary>
    ///     Verifies if an entity can be stored and if it fits
    /// </summary>
    /// <param name="uid">The entity to check</param>
    /// <param name="insertEnt"></param>
    /// <param name="reason">If returning false, the reason displayed to the player</param>
    /// <param name="storageComp"></param>
    /// <param name="item"></param>
    /// <param name="ignoreStacks"></param>
    /// <param name="ignoreLocation"></param>
    /// <returns>true if it can be inserted, false otherwise</returns>
    public bool CanInsert(
        EntityUid uid,
        EntityUid insertEnt,
        out string? reason,
        StorageComponent? storageComp = null,
        ItemComponent? item = null,
        bool ignoreStacks = false,
        bool ignoreLocation = false)
    {
        if (!Resolve(uid, ref storageComp) || !Resolve(insertEnt, ref item, false))
        {
            reason = null;
            return false;
        }

        if (Transform(insertEnt).Anchored)
        {
            reason = "comp-storage-anchored-failure";
            return false;
        }

        if (storageComp.Whitelist?.IsValid(insertEnt, EntityManager) == false)
        {
            reason = "comp-storage-invalid-container";
            return false;
        }

        if (storageComp.Blacklist?.IsValid(insertEnt, EntityManager) == true)
        {
            reason = "comp-storage-invalid-container";
            return false;
        }

        if (!ignoreStacks
            && _stackQuery.TryGetComponent(insertEnt, out var stack)
            && HasSpaceInStacks((uid, storageComp), stack.StackTypeId))
        {
            reason = null;
            return true;
        }

        var maxSize = GetMaxItemSize((uid, storageComp));
        if (ItemSystem.GetSizePrototype(item.Size) > maxSize)
        {
            reason = "comp-storage-too-big";
            return false;
        }

        if (TryComp<StorageComponent>(insertEnt, out var insertStorage)
            && GetMaxItemSize((insertEnt, insertStorage)) >= maxSize)
        {
            reason = "comp-storage-too-big";
            return false;
        }

        if (!ignoreLocation && !storageComp.StoredItems.ContainsKey(insertEnt))
        {
            if (!TryGetAvailableGridSpace((uid, storageComp), (insertEnt, item), out _))
            {
                reason = "comp-storage-insufficient-capacity";
                return false;
            }
        }

        CheckingCanInsert = true;
        if (!_containerSystem.CanInsert(insertEnt, storageComp.Container))
        {
            CheckingCanInsert = false;
            reason = null;
            return false;
        }
        CheckingCanInsert = false;

        reason = null;
        return true;
    }

    /// <summary>
    ///     Inserts into the storage container at a given location
    /// </summary>
    /// <returns>true if the entity was inserted, false otherwise. This will also return true if a stack was partially
    /// inserted.</returns>
    public bool InsertAt(
        Entity<StorageComponent?> uid,
        Entity<ItemComponent?> insertEnt,
        ItemStorageLocation location,
        out EntityUid? stackedEntity,
        EntityUid? user = null,
        bool playSound = true,
        bool stackAutomatically = true)
    {
        stackedEntity = null;
        if (!Resolve(uid, ref uid.Comp))
            return false;

        if (!ItemFitsInGridLocation(insertEnt, uid, location))
            return false;

        uid.Comp.StoredItems[insertEnt] = location;
        Dirty(uid, uid.Comp);

        if (Insert(uid,
                insertEnt,
                out stackedEntity,
                out _,
                user: user,
                storageComp: uid.Comp,
                playSound: playSound,
                stackAutomatically: stackAutomatically))
        {
            return true;
        }

        uid.Comp.StoredItems.Remove(insertEnt);
        return false;
    }

    /// <summary>
    ///     Inserts into the storage container
    /// </summary>
    /// <returns>true if the entity was inserted, false otherwise. This will also return true if a stack was partially
    /// inserted.</returns>
    public bool Insert(
        EntityUid uid,
        EntityUid insertEnt,
        out EntityUid? stackedEntity,
        EntityUid? user = null,
        StorageComponent? storageComp = null,
        bool playSound = true,
        bool stackAutomatically = true)
    {
        return Insert(uid, insertEnt, out stackedEntity, out _, user: user, storageComp: storageComp, playSound: playSound, stackAutomatically: stackAutomatically);
    }

    /// <summary>
    ///     Inserts into the storage container
    /// </summary>
    /// <returns>true if the entity was inserted, false otherwise. This will also return true if a stack was partially
    /// inserted</returns>
    public bool Insert(
        EntityUid uid,
        EntityUid insertEnt,
        out EntityUid? stackedEntity,
        out string? reason,
        EntityUid? user = null,
        StorageComponent? storageComp = null,
        bool playSound = true,
        bool stackAutomatically = true)
    {
        stackedEntity = null;
        reason = null;

        if (!Resolve(uid, ref storageComp))
            return false;

        /*
         * 1. If the inserted thing is stackable then try to stack it to existing stacks
         * 2. If anything remains insert whatever is possible.
         * 3. If insertion is not possible then leave the stack as is.
         * At either rate still play the insertion sound
         *
         * For now we just treat items as always being the same size regardless of stack count.
         */

        if (!stackAutomatically || !_stackQuery.TryGetComponent(insertEnt, out var insertStack))
        {
            if (!_containerSystem.Insert(insertEnt, storageComp.Container))
                return false;

            if (playSound)
                Audio.PlayPredicted(storageComp.StorageInsertSound, uid, user);

            return true;
        }

        var toInsertCount = insertStack.Count;

        foreach (var ent in storageComp.Container.ContainedEntities)
        {
            if (!_stackQuery.TryGetComponent(ent, out var containedStack))
                continue;

            if (!_stack.TryAdd(insertEnt, ent, insertStack, containedStack))
                continue;

            stackedEntity = ent;
            if (insertStack.Count == 0)
                break;
        }

        // Still stackable remaining
        if (insertStack.Count > 0
            && !_containerSystem.Insert(insertEnt, storageComp.Container)
            && toInsertCount == insertStack.Count)
        {
            // Failed to insert anything.
            return false;
        }

        if (playSound)
            Audio.PlayPredicted(storageComp.StorageInsertSound, uid, user);

        return true;
    }

    /// <summary>
    ///     Inserts an entity into storage from the player's active hand
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="player">The player to insert an entity from</param>
    /// <param name="storageComp"></param>
    /// <returns>true if inserted, false otherwise</returns>
    public bool PlayerInsertHeldEntity(EntityUid uid, EntityUid player, StorageComponent? storageComp = null)
    {
        if (!Resolve(uid, ref storageComp) || !TryComp(player, out HandsComponent? hands) || hands.ActiveHandEntity == null)
            return false;

        var toInsert = hands.ActiveHandEntity;

        if (!CanInsert(uid, toInsert.Value, out var reason, storageComp))
        {
            _popupSystem.PopupClient(Loc.GetString(reason ?? "comp-storage-cant-insert"), uid, player);
            return false;
        }

        if (!_sharedHandsSystem.CanDrop(player, toInsert.Value, hands))
        {
            _popupSystem.PopupClient(Loc.GetString("comp-storage-cant-drop", ("entity", toInsert.Value)), uid, player);
            return false;
        }

        return PlayerInsertEntityInWorld((uid, storageComp), player, toInsert.Value);
    }

    /// <summary>
    ///     Inserts an Entity (<paramref name="toInsert"/>) in the world into storage, informing <paramref name="player"/> if it fails.
    ///     <paramref name="toInsert"/> is *NOT* held, see <see cref="PlayerInsertHeldEntity(EntityUid,EntityUid,StorageComponent)"/>.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="player">The player to insert an entity with</param>
    /// <param name="toInsert"></param>
    /// <returns>true if inserted, false otherwise</returns>
    public bool PlayerInsertEntityInWorld(Entity<StorageComponent?> uid, EntityUid player, EntityUid toInsert)
    {
        if (!Resolve(uid, ref uid.Comp) || !_interactionSystem.InRangeUnobstructed(player, uid))
            return false;

        if (!Insert(uid, toInsert, out _, user: player, uid.Comp))
        {
            _popupSystem.PopupClient(Loc.GetString("comp-storage-cant-insert"), uid, player);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Attempts to set the location of an item already inside of a storage container.
    /// </summary>
    public bool TrySetItemStorageLocation(Entity<ItemComponent?> itemEnt, Entity<StorageComponent?> storageEnt, ItemStorageLocation location)
    {
        if (!Resolve(itemEnt, ref itemEnt.Comp) || !Resolve(storageEnt, ref storageEnt.Comp))
            return false;

        if (!storageEnt.Comp.Container.ContainedEntities.Contains(itemEnt))
            return false;

        if (!ItemFitsInGridLocation(itemEnt, storageEnt, location.Position, location.Rotation))
            return false;

        storageEnt.Comp.StoredItems[itemEnt] = location;
        Dirty(storageEnt, storageEnt.Comp);
        return true;
    }

    /// <summary>
    /// Tries to find the first available spot on a storage grid.
    /// starts at the top-left and goes right and down.
    /// </summary>
    public bool TryGetAvailableGridSpace(
        Entity<StorageComponent?> storageEnt,
        Entity<ItemComponent?> itemEnt,
        [NotNullWhen(true)] out ItemStorageLocation? storageLocation)
    {
        storageLocation = null;

        if (!Resolve(storageEnt, ref storageEnt.Comp) || !Resolve(itemEnt, ref itemEnt.Comp))
            return false;

        // if the item has an available saved location, use that
        if (FindSavedLocation(storageEnt, itemEnt, out storageLocation))
            return true;

        var storageBounding = storageEnt.Comp.Grid.GetBoundingBox();

        Angle startAngle;
        if (storageEnt.Comp.DefaultStorageOrientation == null)
        {
            startAngle = Angle.FromDegrees(-itemEnt.Comp.StoredRotation);
        }
        else
        {
            if (storageBounding.Width < storageBounding.Height)
            {
                startAngle = storageEnt.Comp.DefaultStorageOrientation == StorageDefaultOrientation.Horizontal
                    ? Angle.Zero
                    : Angle.FromDegrees(90);
            }
            else
            {
                startAngle = storageEnt.Comp.DefaultStorageOrientation == StorageDefaultOrientation.Vertical
                    ? Angle.Zero
                    : Angle.FromDegrees(90);
            }
        }

        for (var y = storageBounding.Bottom; y <= storageBounding.Top; y++)
        {
            for (var x = storageBounding.Left; x <= storageBounding.Right; x++)
            {
                for (var angle = startAngle; angle <= Angle.FromDegrees(360 - startAngle); angle += Math.PI / 2f)
                {
                    var location = new ItemStorageLocation(angle, (x, y));
                    if (ItemFitsInGridLocation(itemEnt, storageEnt, location))
                    {
                        storageLocation = location;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Tries to find a saved location for an item from its name.
    /// If none are saved or they are all blocked nothing is returned.
    /// </summary>
    public bool FindSavedLocation(
        Entity<StorageComponent?> ent,
        Entity<ItemComponent?> item,
        [NotNullWhen(true)] out ItemStorageLocation? storageLocation)
    {
        storageLocation = null;
        if (!Resolve(ent, ref ent.Comp))
            return false;

        var name = Name(item);
        if (!ent.Comp.SavedLocations.TryGetValue(name, out var list))
            return false;

        foreach (var location in list)
        {
            if (ItemFitsInGridLocation(item, ent, location))
            {
                storageLocation = location;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Saves an item's location in the grid for later insertion to use.
    /// </summary>
    public void SaveItemLocation(Entity<StorageComponent?> ent, Entity<MetaDataComponent?> item)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        // needs to actually be stored in it somewhere to save it
        if (!ent.Comp.StoredItems.TryGetValue(item, out var location))
            return;

        var name = Name(item, item.Comp);
        if (ent.Comp.SavedLocations.TryGetValue(name, out var list))
        {
            // iterate to make sure its not already been saved
            for (int i = 0; i < list.Count; i++)
            {
                var saved = list[i];
                
                if (saved == location)
                {
                    list.Remove(location);
                    return;
                }
            }

            list.Add(location);
        }
        else
        {
            list = new List<ItemStorageLocation>()
            {
                location
            };
            ent.Comp.SavedLocations[name] = list;
        }

        Dirty(ent, ent.Comp);
    }

    /// <summary>
    /// Checks if an item fits into a specific spot on a storage grid.
    /// </summary>
    public bool ItemFitsInGridLocation(
        Entity<ItemComponent?> itemEnt,
        Entity<StorageComponent?> storageEnt,
        ItemStorageLocation location)
    {
        return ItemFitsInGridLocation(itemEnt, storageEnt, location.Position, location.Rotation);
    }

    /// <summary>
    /// Checks if an item fits into a specific spot on a storage grid.
    /// </summary>
    public bool ItemFitsInGridLocation(
        Entity<ItemComponent?> itemEnt,
        Entity<StorageComponent?> storageEnt,
        Vector2i position,
        Angle rotation)
    {
        if (!Resolve(itemEnt, ref itemEnt.Comp) || !Resolve(storageEnt, ref storageEnt.Comp))
            return false;

        var gridBounds = storageEnt.Comp.Grid.GetBoundingBox();
        if (!gridBounds.Contains(position))
            return false;

        var itemShape = ItemSystem.GetAdjustedItemShape(itemEnt, rotation, position);

        foreach (var box in itemShape)
        {
            for (var offsetY = box.Bottom; offsetY <= box.Top; offsetY++)
            {
                for (var offsetX = box.Left; offsetX <= box.Right; offsetX++)
                {
                    var pos = (offsetX, offsetY);

                    if (!IsGridSpaceEmpty(itemEnt, storageEnt, pos))
                        return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if a space on a grid is valid and not occupied by any other pieces.
    /// </summary>
    public bool IsGridSpaceEmpty(Entity<ItemComponent?> itemEnt, Entity<StorageComponent?> storageEnt, Vector2i location)
    {
        if (!Resolve(storageEnt, ref storageEnt.Comp))
            return false;

        var validGrid = false;
        foreach (var grid in storageEnt.Comp.Grid)
        {
            if (grid.Contains(location))
            {
                validGrid = true;
                break;
            }
        }

        if (!validGrid)
            return false;

        foreach (var (ent, storedItem) in storageEnt.Comp.StoredItems)
        {
            if (ent == itemEnt.Owner)
                continue;

            if (!_itemQuery.TryGetComponent(ent, out var itemComp))
                continue;

            var adjustedShape = ItemSystem.GetAdjustedItemShape((ent, itemComp), storedItem);
            foreach (var box in adjustedShape)
            {
                if (box.Contains(location))
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns true if there is enough space to theoretically fit another item.
    /// </summary>
    public bool HasSpace(Entity<StorageComponent?> uid)
    {
        if (!Resolve(uid, ref uid.Comp))
            return false;

        return GetCumulativeItemAreas(uid) < uid.Comp.Grid.GetArea() || HasSpaceInStacks(uid);
    }

    private bool HasSpaceInStacks(Entity<StorageComponent?> uid, string? stackType = null)
    {
        if (!Resolve(uid, ref uid.Comp))
            return false;

        foreach (var contained in uid.Comp.Container.ContainedEntities)
        {
            if (!_stackQuery.TryGetComponent(contained, out var stack))
                continue;

            if (stackType != null && !stack.StackTypeId.Equals(stackType))
                continue;

            if (_stack.GetAvailableSpace(stack) == 0)
                continue;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns the sum of all the ItemSizes of the items inside of a storage.
    /// </summary>
    public int GetCumulativeItemAreas(Entity<StorageComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return 0;

        var sum = 0;
        foreach (var item in entity.Comp.Container.ContainedEntities)
        {
            if (!_itemQuery.TryGetComponent(item, out var itemComp))
                continue;
            sum += ItemSystem.GetItemShape((item, itemComp)).GetArea();
        }

        return sum;
    }

    public ItemSizePrototype GetMaxItemSize(Entity<StorageComponent?> uid)
    {
        if (!Resolve(uid, ref uid.Comp))
            return _defaultStorageMaxItemSize;

        // If we specify a max item size, use that
        if (uid.Comp.MaxItemSize != null)
            return _prototype.Index(uid.Comp.MaxItemSize.Value);

        if (!_itemQuery.TryGetComponent(uid, out var item))
            return _defaultStorageMaxItemSize;

        // if there is no max item size specified, the value used
        // is one below the item size of the storage entity.
        return _nextSmallest[item.Size];
    }

    private void OnStackCountChanged(EntityUid uid, MetaDataComponent component, StackCountChangedEvent args)
    {
        if (_containerSystem.TryGetContainingContainer(uid, out var container, component) &&
            container.ID == StorageComponent.ContainerId)
        {
            UpdateAppearance(container.Owner);
            UpdateUI(container.Owner);
        }
    }

    /// <summary>
    /// Plays a clientside pickup animation for the specified uid.
    /// </summary>
    public abstract void PlayPickupAnimation(EntityUid uid, EntityCoordinates initialCoordinates,
        EntityCoordinates finalCoordinates, Angle initialRotation, EntityUid? user = null);

    [Serializable, NetSerializable]
    protected sealed class StorageComponentState : ComponentState
    {
        public bool IsUiOpen;

        public Dictionary<NetEntity, ItemStorageLocation> StoredItems = new();

        public Dictionary<string, List<ItemStorageLocation>> SavedLocations = new();

        public List<Box2i> Grid = new();

        public ProtoId<ItemSizePrototype>? MaxItemSize;
    }
}
