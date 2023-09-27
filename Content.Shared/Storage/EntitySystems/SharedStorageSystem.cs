using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.CombatMode;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
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
using Robust.Shared.Random;

namespace Content.Shared.Storage.EntitySystems;

public abstract class SharedStorageSystem : EntitySystem
{
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] private   readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private   readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private   readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] protected readonly SharedEntityStorageSystem EntityStorage = default!;
    [Dependency] private   readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] private   readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private   readonly SharedHandsSystem _sharedHandsSystem = default!;
    [Dependency] private   readonly SharedInteractionSystem _sharedInteractionSystem = default!;
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

        SubscribeLocalEvent<StorageComponent, EntInsertedIntoContainerMessage>(OnStorageItemInserted);
        SubscribeLocalEvent<StorageComponent, EntRemovedFromContainerMessage>(OnStorageItemRemoved);

        SubscribeLocalEvent<StorageComponent, AreaPickupDoAfterEvent>(OnDoAfter);

        SubscribeLocalEvent<StorageComponent, StorageInteractWithItemEvent>(OnInteractWithItem);
    }

    private void OnComponentInit(EntityUid uid, StorageComponent storageComp, ComponentInit args)
    {
        // ReSharper disable once StringLiteralTypo
        storageComp.Container = _containerSystem.EnsureContainer<Container>(uid, "storagebase");
        UpdateStorage(uid, storageComp);
    }

    /// <summary>
    /// Updates the storage UI, visualizer, etc.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    private void UpdateStorage(EntityUid uid, StorageComponent component)
    {
        // TODO: I had this.
        // We can get states being applied before the container is ready.
        if (component.Container == default)
            return;

        RecalculateStorageUsed(uid, component);
        UpdateStorageVisualization(uid, component);
        UpdateUI(uid, component);
        Dirty(uid, component);
    }

    public virtual void UpdateUI(EntityUid uid, StorageComponent component) {}

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
            && (!TryComp(uid, out LockComponent? targetLock) || !targetLock.Locked))
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

        Log.Debug($"Storage (UID {uid}) attacked by user (UID {args.User}) with entity (UID {args.Used}).");

        if (HasComp<PlaceableSurfaceComponent>(uid))
            return;

        PlayerInsertHeldEntity(uid, args.User, storageComp);
        // Always handle it, even if insertion fails.
        // We don't want to trigger any AfterInteract logic here.
        // Example bug: placing wires if item doesn't fit in backpack.
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
        if (!args.CanReach)
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

            if (TryComp<TransformComponent>(uid, out var transformOwner) && TryComp<TransformComponent>(target, out var transformEnt))
            {
                var parent = transformOwner.ParentUid;

                var position = EntityCoordinates.FromMap(
                    parent.IsValid() ? parent : uid,
                    transformEnt.MapPosition,
                    _transform
                );

                if (PlayerInsertEntityInWorld(uid, args.User, target, storageComp))
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

            if (PlayerInsertEntityInWorld(uid, args.Args.User, entity, component))
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
        if (args.Session.AttachedEntity is not EntityUid player)
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
            UpdateStorageVisualization(uid, storageComp);
        }
    }

    private void OnStorageItemInserted(EntityUid uid, StorageComponent component, EntInsertedIntoContainerMessage args)
    {
        UpdateStorage(uid, component);
    }

    private void OnStorageItemRemoved(EntityUid uid, StorageComponent storageComp, EntRemovedFromContainerMessage args)
    {
        UpdateStorage(uid, storageComp);
    }

    protected void UpdateStorageVisualization(EntityUid uid, StorageComponent storageComp)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        _appearance.SetData(uid, StorageVisuals.Open, storageComp.IsUiOpen, appearance);
        _appearance.SetData(uid, SharedBagOpenVisuals.BagState, storageComp.IsUiOpen ? SharedBagState.Open : SharedBagState.Closed);

        if (HasComp<ItemCounterComponent>(uid))
            _appearance.SetData(uid, StackVisuals.Hide, !storageComp.IsUiOpen);
    }

    public void RecalculateStorageUsed(EntityUid uid, StorageComponent storageComp)
    {
        storageComp.StorageUsed = 0;

        foreach (var entity in storageComp.Container.ContainedEntities)
        {
            if (!_itemQuery.TryGetComponent(entity, out var itemComp))
                continue;

            var size = itemComp.Size;
            storageComp.StorageUsed += size;
        }

        _appearance.SetData(uid, StorageVisuals.StorageUsed, storageComp.StorageUsed);
        _appearance.SetData(uid, StorageVisuals.Capacity, storageComp.StorageCapacityMax);
    }

    public int GetAvailableSpace(EntityUid uid, StorageComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return 0;

        return component.StorageCapacityMax - component.StorageUsed;
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
    /// <param name="reason">If returning false, the reason displayed to the player</param>
    /// <returns>true if it can be inserted, false otherwise</returns>
    public bool CanInsert(EntityUid uid, EntityUid insertEnt, out string? reason, StorageComponent? storageComp = null)
    {
        if (!Resolve(uid, ref storageComp))
        {
            reason = null;
            return false;
        }

        if (TryComp(insertEnt, out TransformComponent? transformComp) && transformComp.Anchored)
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

        if (TryComp(insertEnt, out StorageComponent? storage) &&
            storage.StorageCapacityMax >= storageComp.StorageCapacityMax)
        {
            reason = "comp-storage-insufficient-capacity";
            return false;
        }

        if (TryComp(insertEnt, out ItemComponent? itemComp) &&
            itemComp.Size > storageComp.StorageCapacityMax - storageComp.StorageUsed)
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
    /// <returns>true if the entity was inserted, false otherwise</returns>
    public bool Insert(EntityUid uid, EntityUid insertEnt, out EntityUid? stackedEntity, EntityUid? user = null, StorageComponent? storageComp = null, bool playSound = true)
    {
        stackedEntity = null;

        if (!Resolve(uid, ref storageComp) || !CanInsert(uid, insertEnt, out _, storageComp))
            return false;

        /*
         * 1. If the inserted thing is stackable then try to stack it to existing stacks
         * 2. If anything remains insert whatever is possible.
         * 3. If insertion is not possible then leave the stack as is.
         * At either rate still play the insertion sound
         *
         * For now we just treat items as always being the same size regardless of stack count.
         */

        // If it's stackable then prefer to stack it
        if (_stackQuery.TryGetComponent(insertEnt, out var insertStack))
        {
            var toInsertCount = insertStack.Count;

            foreach (var ent in storageComp.Container.ContainedEntities)
            {
                if (!_stackQuery.TryGetComponent(ent, out var containedStack) || !insertStack.StackTypeId.Equals(containedStack.StackTypeId))
                    continue;

                if (!_stack.TryAdd(insertEnt, ent, insertStack, containedStack))
                    continue;

                stackedEntity = ent;
                var remaining = insertStack.Count;
                toInsertCount -= toInsertCount - remaining;

                if (remaining > 0)
                    continue;

                break;
            }

            // Still stackable remaining
            if (insertStack.Count > 0)
            {
                // Try to insert it as a new stack.
                if (TryComp(insertEnt, out ItemComponent? itemComp) &&
                    itemComp.Size > storageComp.StorageCapacityMax - storageComp.StorageUsed ||
                    !storageComp.Container.Insert(insertEnt))
                {
                    // If we also didn't do any stack fills above then just end
                    // otherwise play sound and update UI anyway.
                    if (toInsertCount == insertStack.Count)
                        return false;
                }
            }
        }
        // Non-stackable but no insertion for reasons.
        else if (!storageComp.Container.Insert(insertEnt))
        {
            return false;
        }

        if (playSound && storageComp.StorageInsertSound is not null)
            Audio.PlayPredicted(storageComp.StorageInsertSound, uid, user);

        return true;
    }

    /// <summary>
    ///     Inserts an entity into storage from the player's active hand
    /// </summary>
    /// <param name="player">The player to insert an entity from</param>
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

        if (!_sharedHandsSystem.TryDrop(player, toInsert.Value, handsComp: hands))
        {
            _popupSystem.PopupClient(Loc.GetString("comp-storage-cant-drop"), uid, player);
            return false;
        }

        return PlayerInsertEntityInWorld(uid, player, toInsert.Value, storageComp);
    }

    /// <summary>
    ///     Inserts an Entity (<paramref name="toInsert"/>) in the world into storage, informing <paramref name="player"/> if it fails.
    ///     <paramref name="toInsert"/> is *NOT* held, see <see cref="PlayerInsertHeldEntity(Robust.Shared.GameObjects.EntityUid)"/>.
    /// </summary>
    /// <param name="player">The player to insert an entity with</param>
    /// <returns>true if inserted, false otherwise</returns>
    public bool PlayerInsertEntityInWorld(EntityUid uid, EntityUid player, EntityUid toInsert, StorageComponent? storageComp = null)
    {
        if (!Resolve(uid, ref storageComp) || !_sharedInteractionSystem.InRangeUnobstructed(player, uid))
            return false;

        if (!Insert(uid, toInsert, out _, user: player, storageComp))
        {
            _popupSystem.PopupClient(Loc.GetString("comp-storage-cant-insert"), uid, player);
            return false;
        }
        return true;
    }

    /// <summary>
    /// Plays a clientside pickup animation for the specified uid.
    /// </summary>
    public abstract void PlayPickupAnimation(EntityUid uid, EntityCoordinates initialCoordinates,
        EntityCoordinates finalCoordinates, Angle initialRotation, EntityUid? user = null);
}
