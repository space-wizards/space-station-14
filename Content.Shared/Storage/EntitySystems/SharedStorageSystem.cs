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
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Storage.EntitySystems;

public abstract class SharedStorageSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] private   readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private   readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private   readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] protected readonly SharedEntityStorageSystem EntityStorage = default!;
    [Dependency] private   readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private   readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private   readonly SharedHandsSystem _sharedHandsSystem = default!;
    [Dependency] private   readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private   readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] private   readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] protected   readonly SharedTransformSystem _transform = default!;
    [Dependency] private   readonly SharedStackSystem _stack = default!;
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
        SubscribeLocalEvent<StorageComponent, StorageComponent.StorageInsertItemMessage>(OnInsertItemMessage);
        SubscribeLocalEvent<StorageComponent, BoundUIOpenedEvent>(OnBoundUIOpen);
        SubscribeLocalEvent<MetaDataComponent, StackCountChangedEvent>(OnStackCountChanged);

        SubscribeLocalEvent<StorageComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<StorageComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<StorageComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);

        SubscribeLocalEvent<StorageComponent, AreaPickupDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<StorageComponent, StorageInteractWithItemEvent>(OnInteractWithItem);
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
    }

    /// <summary>
    /// Specifically for storage implants.
    /// </summary>
    private void OnImplantActivate(EntityUid uid, StorageComponent storageComp, OpenStorageImplantEvent args)
    {
        // TODO: Make this an action or something.
        if (args.Handled || !_xformQuery.TryGetComponent(uid, out var xform))
            return;

        OpenStorageUI(uid, xform.ParentUid, storageComp);
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
                    transformEnt.MapPosition,
                    _transform
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
                new MapCoordinates(_transform.GetWorldPosition(targetXform), targetXform.MapID),
                _transform
            );

            var angle = targetXform.LocalRotation;

            if (PlayerInsertEntityInWorld((uid, component), args.Args.User, entity))
            {
                successfullyInserted.Add(entity);
                successfullyInsertedPositions.Add(position);
                successfullyInsertedAngles.Add(angle);
            }
        }

        // If we picked up atleast one thing, play a sound and do a cool animation!
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
        var coordinates = _transform.GetMoverCoordinates(uid);

        // Being destroyed so need to recalculate.
        _containerSystem.EmptyContainer(storageComp.Container, destination: coordinates);
    }

    /// <summary>
    ///     This function gets called when the user clicked on an item in the storage UI. This will either place the
    ///     item in the user's hand if it is currently empty, or interact with the item using the user's currently
    ///     held item.
    /// </summary>
    private void OnInteractWithItem(EntityUid uid, StorageComponent storageComp, StorageInteractWithItemEvent args)
    {
        if (args.Session.AttachedEntity is not { } player)
            return;

        var entity = GetEntity(args.InteractedItemUID);

        if (!Exists(entity))
        {
            Log.Error($"Player {args.Session} interacted with non-existent item {args.InteractedItemUID} stored in {ToPrettyString(uid)}");
            return;
        }

        if (!_actionBlockerSystem.CanInteract(player, entity) || !storageComp.Container.Contains(entity))
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

    private void OnInsertItemMessage(EntityUid uid, StorageComponent storageComp, StorageComponent.StorageInsertItemMessage args)
    {
        if (args.Session.AttachedEntity == null)
            return;

        PlayerInsertHeldEntity(uid, args.Session.AttachedEntity.Value, storageComp);
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
        if (_net.IsServer)
        {
            if (!entity.Comp.StoredItems.ContainsKey(GetNetEntity(entity.Owner)))
            {
                if (!TryGetAvailableGridSpace((entity.Owner, entity.Comp), (args.Entity, null), out var location))
                {
                    _containerSystem.Remove(args.Entity, args.Container, force: true);
                    return;
                }

                entity.Comp.StoredItems[GetNetEntity(args.Entity)] = location.Value;
                Dirty(entity, entity.Comp);
            }
        }

        OnContainerModified(entity.Owner, entity.Comp, args);
    }

    private void OnEntRemoved(Entity<StorageComponent> entity, ref EntRemovedFromContainerMessage args)
    {
        entity.Comp.StoredItems.Remove(GetNetEntity(args.Entity));
        Dirty(entity, entity.Comp);

        OnContainerModified(entity.Owner, entity.Comp, args);
    }

    //todo we gotta remove this at some point, too
    private void OnContainerModified(EntityUid uid, StorageComponent component, ContainerModifiedMessage args)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (component.Container == null)
            return;

        if (args.Container.ID != StorageComponent.ContainerId)
            return;

        UpdateAppearance((uid, component, null));
        UpdateUI((uid, component));
    }

    private void OnInsertAttempt(EntityUid uid, StorageComponent component, ContainerIsInsertingAttemptEvent args)
    {
        if (args.Cancelled || args.Container.ID != StorageComponent.ContainerId)
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

        int capacity;
        int used;
        if (storage.MaxSlots == null)
        {
            used = GetCumulativeItemSizes(uid, storage);
            capacity = storage.MaxTotalWeight;
        }
        else
        {
            capacity = storage.MaxSlots.Value;
            used = storage.Container.ContainedEntities.Count;
        }

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
    /// <returns>true if it can be inserted, false otherwise</returns>
    public bool CanInsert(EntityUid uid, EntityUid insertEnt, out string? reason, StorageComponent? storageComp = null, ItemComponent? item = null, bool ignoreStacks = false)
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

        var maxSize = _item.GetSizePrototype(GetMaxItemSize((uid, storageComp)));
        if (_item.GetSizePrototype(item.Size) > maxSize)
        {
            reason = "comp-storage-too-big";
            return false;
        }

        if (TryComp<StorageComponent>(insertEnt, out var insertStorage)
            && _item.GetSizePrototype(GetMaxItemSize((insertEnt, insertStorage))) >= maxSize)
        {
            reason = "comp-storage-too-big";
            return false;
        }

        if (!TryGetAvailableGridSpace((uid, storageComp), (insertEnt, item), out _))
        {
            reason = "comp-storage-insufficient-capacity";
            return false;
        }

        if (storageComp.MaxSlots != null)
        {
            if (storageComp.Container.ContainedEntities.Count >= storageComp.MaxSlots)
            {
                reason = "comp-storage-insufficient-capacity";
                return false;
            }
        }
        else if (_item.GetItemSizeWeight(item.Size) + GetCumulativeItemSizes(uid, storageComp) > storageComp.MaxTotalWeight)
        {
            reason = "comp-storage-insufficient-capacity";
            return false;
        }

        reason = null;
        return true;
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
        bool playSound = true)
    {
        return Insert(uid, insertEnt, out stackedEntity, out _, user: user, storageComp: storageComp, playSound: playSound);
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
        bool playSound = true)
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

        if (!_stackQuery.TryGetComponent(insertEnt, out var insertStack))
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

    public bool TryGetAvailableGridSpace(
        Entity<StorageComponent?> storageEnt,
        Entity<ItemComponent?> itemEnt,
        [NotNullWhen(true)] out ItemStorageLocation? storageLocation)
    {
        storageLocation = null;

        if (!Resolve(storageEnt, ref storageEnt.Comp) || !Resolve(itemEnt, ref itemEnt.Comp))
            return false;

        var baseItemShape = _item.GetItemShape(itemEnt);
        var itemBounding = GetBoundingBox(baseItemShape);
        var storageBounding = GetBoundingBox(storageEnt.Comp.StorageGrid);

        //todo this is gonna need to support automatic rotation, theoretically.
        for (var y = storageBounding.Bottom; y <= storageBounding.Top; y++)
        {
            if (y + itemBounding.Height > storageBounding.Top)
                break;

            for (var x = storageBounding.Left; x <= storageBounding.Right; x++)
            {
                if (x + itemBounding.Width > storageBounding.Right)
                    break;

                var validPosition = true;

                foreach (var box in baseItemShape)
                {
                    if (!validPosition)
                        break;

                    for (var offsetY = box.Bottom; offsetY <= box.Top; offsetY++)
                    {
                        if (!validPosition)
                            break;

                        for (var offsetX = box.Left; offsetX <= box.Right; offsetX++)
                        {
                            var position = (x + offsetX, y + offsetY);

                            if (IsGridSpaceEmpty(storageEnt, position))
                                continue;

                            validPosition = false;
                            break;
                        }
                    }
                }

                if (validPosition)
                {
                    storageLocation = new ItemStorageLocation(Angle.Zero, (x, y));
                    return true;
                }
            }
        }

        return storageLocation != null;
    }

    public bool IsGridSpaceEmpty(Entity<StorageComponent?> entity, Vector2i location)
    {
        if (!Resolve(entity, ref entity.Comp))
            return false;

        var validGrid = false;
        foreach (var grid in entity.Comp.StorageGrid)
        {
            if (grid.Contains(location))
            {
                validGrid = true;
                break;
            }
        }

        if (!validGrid)
            return false;

        foreach (var (ent, storedItem) in entity.Comp.StoredItems)
        {
            //todo add a fucking trycomp for itemcomponent you stingy bitch
            var adjustedShape = _item.GetAdjustedItemShape((GetEntity(ent), null), storedItem);
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

        //todo maybe this shouldn't be authoritative over weight? idk.
        if (uid.Comp.MaxSlots != null)
        {
            return uid.Comp.Container.ContainedEntities.Count < uid.Comp.MaxSlots || HasSpaceInStacks(uid);
        }

        return GetCumulativeItemSizes(uid, uid.Comp) < uid.Comp.MaxTotalWeight || HasSpaceInStacks(uid);
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
    public int GetCumulativeItemSizes(EntityUid uid, StorageComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return 0;

        var sum = 0;
        foreach (var item in component.Container.ContainedEntities)
        {
            if (!_itemQuery.TryGetComponent(item, out var itemComp))
                continue;
            sum += _item.GetItemSizeWeight(itemComp.Size);
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
        var size = _item.GetSizePrototype(item.Size);

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

    //todo make an engine pr you bitch
    public static Box2i GetBoundingBox(IReadOnlyList<Box2i> boxes)
    {
        if (boxes.Count == 0)
            return new Box2i();

        var minBottom = boxes.Min(x => x.Bottom);
        var minLeft = boxes.Min(x => x.Left);
        var maxTop = boxes.Max(x => x.Top);
        var maxRight = boxes.Max(x => x.Right);
        return new Box2i(new Vector2i(minLeft, minBottom), new Vector2i(maxRight, maxTop));
    }
}
