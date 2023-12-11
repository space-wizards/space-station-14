using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.CombatMode;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Lock;
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Storage.Components;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Storage.EntitySystems;

public abstract class SharedStorageSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] private   readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private   readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private   readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] protected readonly SharedEntityStorageSystem EntityStorage = default!;
    [Dependency] private   readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] protected readonly SharedItemSystem ItemSystem = default!;
    [Dependency] private   readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private   readonly SharedHandsSystem _sharedHandsSystem = default!;
    [Dependency] protected readonly ActionBlockerSystem ActionBlocker = default!;
    [Dependency] private   readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] private   readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] protected   readonly SharedTransformSystem TransformSystem = default!;
    [Dependency] private   readonly SharedStackSystem _stack = default!;
    [Dependency] private   readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] protected readonly UseDelaySystem UseDelay = default!;

    private EntityQuery<ItemComponent> _itemQuery;
    private EntityQuery<StackComponent> _stackQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    [ValidatePrototypeId<ItemSizePrototype>]
    public const string DefaultStorageMaxItemSize = "Normal";

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        _itemQuery = GetEntityQuery<ItemComponent>();
        _stackQuery = GetEntityQuery<StackComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

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
        if (args.Handled || _combatMode.IsInCombatMode(args.User) || TryComp(uid, out LockComponent? lockComponent) && lockComponent.Locked)
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

            foreach (var entity in _entityLookupSystem.GetEntitiesInRange(args.ClickLocation, storageComp.AreaInsertRadius, LookupFlags.Dynamic | LookupFlags.Sundries))
            {
                if (entity == args.User
                    || !_itemQuery.HasComponent(entity)
                    || !CanInsert(uid, entity, out _, storageComp)
                    || !_interactionSystem.InRangeUnobstructed(args.User, entity))
                {
                    continue;
                }

                validStorables.Add(entity);
            }

            //If there's only one then let's be generous
            if (validStorables.Count > 1)
            {
                var doAfterArgs = new DoAfterArgs(EntityManager, args.User, 0.2f * validStorables.Count, new AreaPickupDoAfterEvent(GetNetEntityList(validStorables)), uid, target: uid)
                {
                    BreakOnDamage = true,
                    BreakOnUserMove = true,
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

        if (!entity.Comp.StoredItems.ContainsKey(GetNetEntity(args.Entity)))
        {
            if (!TryGetAvailableGridSpace((entity.Owner, entity.Comp), (args.Entity, null), out var location))
            {
                _containerSystem.Remove(args.Entity, args.Container, force: true);
                return;
            }

            entity.Comp.StoredItems[GetNetEntity(args.Entity)] = location.Value;
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

        entity.Comp.StoredItems.Remove(GetNetEntity(args.Entity));
        Dirty(entity, entity.Comp);

        UpdateAppearance((entity, entity.Comp, null));
        UpdateUI((entity, entity.Comp));
    }

    private void OnInsertAttempt(EntityUid uid, StorageComponent component, ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled || args.Container.ID != StorageComponent.ContainerId)
            return;

        if (!CanInsert(uid, args.EntityUid, out _, component, ignoreStacks: true, includeContainerChecks: false))
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
        bool ignoreLocation = false,
        bool includeContainerChecks = true)
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

        var maxSize = ItemSystem.GetSizePrototype(GetMaxItemSize((uid, storageComp)));
        if (ItemSystem.GetSizePrototype(item.Size) > maxSize)
        {
            reason = "comp-storage-too-big";
            return false;
        }

        if (TryComp<StorageComponent>(insertEnt, out var insertStorage)
            && ItemSystem.GetSizePrototype(GetMaxItemSize((insertEnt, insertStorage))) >= maxSize)
        {
            reason = "comp-storage-too-big";
            return false;
        }

        if (!ignoreLocation && !storageComp.StoredItems.ContainsKey(GetNetEntity(insertEnt)))
        {
            if (!TryGetAvailableGridSpace((uid, storageComp), (insertEnt, item), out _))
            {
                reason = "comp-storage-insufficient-capacity";
                return false;
            }
        }

        if (includeContainerChecks && !_containerSystem.CanInsert(insertEnt, storageComp.Container))
        {
            reason = null;
            return false;
        }

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

        uid.Comp.StoredItems[GetNetEntity(insertEnt)] = location;
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

        uid.Comp.StoredItems.Remove(GetNetEntity(insertEnt));
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
            _popupSystem.PopupClient(Loc.GetString("comp-storage-cant-drop"), uid, player);
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

        storageEnt.Comp.StoredItems[GetNetEntity(itemEnt)] = location;
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

        var storageBounding = storageEnt.Comp.Grid.GetBoundingBox();

        for (var y = storageBounding.Bottom; y <= storageBounding.Top; y++)
        {
            for (var x = storageBounding.Left; x <= storageBounding.Right; x++)
            {
                for (var angle = Angle.FromDegrees(-itemEnt.Comp.StoredRotation); angle <= Angle.FromDegrees(360 - itemEnt.Comp.StoredRotation); angle += Math.PI / 2f)
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

        foreach (var (netEnt, storedItem) in storageEnt.Comp.StoredItems)
        {
            var ent = GetEntity(netEnt);

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

    public ProtoId<ItemSizePrototype> GetMaxItemSize(Entity<StorageComponent?> uid)
    {
        if (!Resolve(uid, ref uid.Comp))
            return DefaultStorageMaxItemSize;

        // If we specify a max item size, use that
        if (uid.Comp.MaxItemSize != null)
            return uid.Comp.MaxItemSize.Value;

        if (!_itemQuery.TryGetComponent(uid, out var item))
            return DefaultStorageMaxItemSize;
        var size = ItemSystem.GetSizePrototype(item.Size);

        // if there is no max item size specified, the value used
        // is one below the item size of the storage entity, clamped at ItemSize.Tiny
        var sizes = _prototype.EnumeratePrototypes<ItemSizePrototype>().ToList();
        sizes.Sort();
        var currentSizeIndex = sizes.IndexOf(size);
        return sizes[Math.Max(currentSizeIndex - 1, 0)].ID;
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
}
