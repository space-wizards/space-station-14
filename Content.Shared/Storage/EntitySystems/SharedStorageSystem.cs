using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Implants.Components;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Lock;
using Content.Shared.Materials;
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Storage.Components;
using Content.Shared.Timing;
using Content.Shared.Storage.Events;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Storage.EntitySystems;

public abstract class SharedStorageSystem : EntitySystem
{
    [Dependency] private   readonly IConfigurationManager _cfg = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private   readonly IPrototypeManager _prototype = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] private   readonly ISharedAdminLogManager _adminLog = default!;

    [Dependency] protected readonly ActionBlockerSystem ActionBlocker = default!;
    [Dependency] private   readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private   readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private   readonly InventorySystem _inventory = default!;
    [Dependency] private   readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedContainerSystem ContainerSystem = default!;
    [Dependency] private   readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] protected readonly SharedEntityStorageSystem EntityStorage = default!;
    [Dependency] private   readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] protected readonly SharedItemSystem ItemSystem = default!;
    [Dependency] private   readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private   readonly SharedHandsSystem _sharedHandsSystem = default!;
    [Dependency] private   readonly SharedStackSystem _stack = default!;
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;
    [Dependency] protected readonly SharedUserInterfaceSystem UI = default!;
    [Dependency] protected readonly UseDelaySystem UseDelay = default!;

    private EntityQuery<ItemComponent> _itemQuery;
    private EntityQuery<StackComponent> _stackQuery;
    private EntityQuery<TransformComponent> _xformQuery;
    private EntityQuery<UserInterfaceUserComponent> _userQuery;

    /// <summary>
    /// Whether we're allowed to go up-down storage via UI.
    /// </summary>
    public bool NestedStorage = true;

    [ValidatePrototypeId<ItemSizePrototype>]
    public const string DefaultStorageMaxItemSize = "Normal";

    public const float AreaInsertDelayPerItem = 0.075f;
    private static AudioParams _audioParams = AudioParams.Default
        .WithMaxDistance(7f)
        .WithVolume(-2f);

    private ItemSizePrototype _defaultStorageMaxItemSize = default!;

    /// <summary>
    /// Flag for whether we're checking for nested storage interactions.
    /// </summary>
    private bool _nestedCheck;

    public bool CheckingCanInsert;

    private readonly List<EntityUid> _entList = new();
    private readonly HashSet<EntityUid> _entSet = new();

    private readonly List<ItemSizePrototype> _sortedSizes = new();
    private FrozenDictionary<string, ItemSizePrototype> _nextSmallest = FrozenDictionary<string, ItemSizePrototype>.Empty;

    private const string QuickInsertUseDelayID = "quickInsert";
    private const string OpenUiUseDelayID = "storage";

    /// <summary>
    /// How many storage windows are allowed to be open at once.
    /// </summary>
    private int _openStorageLimit = -1;

    protected readonly List<string> CantFillReasons = [];

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        _itemQuery = GetEntityQuery<ItemComponent>();
        _stackQuery = GetEntityQuery<StackComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();
        _userQuery = GetEntityQuery<UserInterfaceUserComponent>();
        _prototype.PrototypesReloaded += OnPrototypesReloaded;

        Subs.CVar(_cfg, CCVars.StorageLimit, OnStorageLimitChanged, true);

        Subs.BuiEvents<StorageComponent>(StorageComponent.StorageUiKey.Key, subs =>
        {
            subs.Event<BoundUIClosedEvent>(OnBoundUIClosed);
        });

        SubscribeLocalEvent<StorageComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<StorageComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<StorageComponent, GetVerbsEvent<ActivationVerb>>(AddUiVerb);
        SubscribeLocalEvent<StorageComponent, ComponentGetState>(OnStorageGetState);
        SubscribeLocalEvent<StorageComponent, ComponentInit>(OnComponentInit, before: new[] { typeof(SharedContainerSystem) });
        SubscribeLocalEvent<StorageComponent, GetVerbsEvent<UtilityVerb>>(AddTransferVerbs);
        SubscribeLocalEvent<StorageComponent, InteractUsingEvent>(OnInteractUsing, after: new[] { typeof(ItemSlotsSystem) });
        SubscribeLocalEvent<StorageComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<StorageComponent, OpenStorageImplantEvent>(OnImplantActivate);
        SubscribeLocalEvent<StorageComponent, AfterInteractEvent>(AfterInteract);
        SubscribeLocalEvent<StorageComponent, DestructionEventArgs>(OnDestroy);
        SubscribeLocalEvent<BoundUserInterfaceMessageAttempt>(OnBoundUIAttempt);
        SubscribeLocalEvent<StorageComponent, BoundUIOpenedEvent>(OnBoundUIOpen);
        SubscribeLocalEvent<StorageComponent, LockToggledEvent>(OnLockToggled);
        SubscribeLocalEvent<MetaDataComponent, StackCountChangedEvent>(OnStackCountChanged);

        SubscribeLocalEvent<StorageComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<StorageComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
        SubscribeLocalEvent<StorageComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);

        SubscribeLocalEvent<StorageComponent, AreaPickupDoAfterEvent>(OnDoAfter);

        SubscribeAllEvent<OpenNestedStorageEvent>(OnStorageNested);
        SubscribeAllEvent<StorageTransferItemEvent>(OnStorageTransfer);
        SubscribeAllEvent<StorageInteractWithItemEvent>(OnInteractWithItem);
        SubscribeAllEvent<StorageSetItemLocationEvent>(OnSetItemLocation);
        SubscribeAllEvent<StorageInsertItemIntoLocationEvent>(OnInsertItemIntoLocation);
        SubscribeAllEvent<StorageSaveItemLocationEvent>(OnSaveItemLocation);

        SubscribeLocalEvent<StorageComponent, GotReclaimedEvent>(OnReclaimed);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenBackpack, InputCmdHandler.FromDelegate(HandleOpenBackpack, handle: false))
            .Bind(ContentKeyFunctions.OpenBelt, InputCmdHandler.FromDelegate(HandleOpenBelt, handle: false))
            .Register<SharedStorageSystem>();

        Subs.CVar(_cfg, CCVars.NestedStorage, OnNestedStorageCvar, true);

        UpdatePrototypeCache();
    }

    private void OnNestedStorageCvar(bool obj)
    {
        NestedStorage = obj;
    }

    private void OnStorageLimitChanged(int obj)
    {
        _openStorageLimit = obj;
    }

    private void OnRemove(Entity<StorageComponent> entity, ref ComponentRemove args)
    {
        UI.CloseUi(entity.Owner, StorageComponent.StorageUiKey.Key);
    }

    private void OnMapInit(Entity<StorageComponent> entity, ref MapInitEvent args)
    {
        UseDelay.SetLength(entity.Owner, entity.Comp.QuickInsertCooldown, QuickInsertUseDelayID);
        UseDelay.SetLength(entity.Owner, entity.Comp.OpenUiCooldown, OpenUiUseDelayID);
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
            MaxItemSize = component.MaxItemSize,
            StoredItems = storedItems,
            SavedLocations = component.SavedLocations,
            Whitelist = component.Whitelist,
            Blacklist = component.Blacklist
        };
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
        storageComp.Container = ContainerSystem.EnsureContainer<Container>(uid, StorageComponent.ContainerId);
        UpdateAppearance((uid, storageComp, null));
    }

    /// <summary>
    ///     If the user has nested-UIs open (e.g., PDA UI open when pda is in a backpack), close them.
    /// </summary>
    private void CloseNestedInterfaces(EntityUid uid, EntityUid actor, StorageComponent? storageComp = null)
    {
        if (!Resolve(uid, ref storageComp))
            return;

        // for each containing thing
        // if it has a storage comp
        // ensure unsubscribe from session
        // if it has a ui component
        // close ui
        foreach (var entity in storageComp.Container.ContainedEntities)
        {
            UI.CloseUis(entity, actor);
        }
    }

    private void OnBoundUIClosed(EntityUid uid, StorageComponent storageComp, BoundUIClosedEvent args)
    {
        CloseNestedInterfaces(uid, args.Actor, storageComp);

        // If UI is closed for everyone
        if (!UI.IsUiOpen(uid, args.UiKey))
        {
            UpdateAppearance((uid, storageComp, null));
            Audio.PlayPredicted(storageComp.StorageCloseSound, uid, args.Actor);
        }
    }

    private void AddUiVerb(EntityUid uid, StorageComponent component, GetVerbsEvent<ActivationVerb> args)
    {
        if (!CanInteract(args.User, (uid, component), args.CanAccess && args.CanInteract))
            return;

        // Does this player currently have the storage UI open?
        var uiOpen = UI.IsUiOpen(uid, StorageComponent.StorageUiKey.Key, args.User);

        ActivationVerb verb = new()
        {
            Act = () =>
            {
                if (uiOpen)
                {
                    UI.CloseUi(uid, StorageComponent.StorageUiKey.Key, args.User);
                }
                else
                {
                    OpenStorageUI(uid, args.User, component);
                }
            }
        };

        if (uiOpen)
        {
            verb.Text = Loc.GetString("comp-storage-verb-close-storage");
            verb.Icon = new SpriteSpecifier.Texture(
                new("/Textures/Interface/VerbIcons/close.svg.192dpi.png"));
        }
        else
        {
            verb.Text = Loc.GetString("comp-storage-verb-open-storage");
            verb.Icon = new SpriteSpecifier.Texture(
                new("/Textures/Interface/VerbIcons/open.svg.192dpi.png"));
        }
        args.Verbs.Add(verb);
    }

    /// <summary>
    ///     Opens the storage UI for an entity
    /// </summary>
    /// <param name="entity">The entity to open the UI for</param>
    public void OpenStorageUI(EntityUid uid, EntityUid entity, StorageComponent? storageComp = null, bool silent = true)
    {
        if (!Resolve(uid, ref storageComp, false))
            return;

        // prevent spamming bag open / honkerton honk sound
        silent |= TryComp<UseDelayComponent>(uid, out var useDelay) && UseDelay.IsDelayed((uid, useDelay), id: OpenUiUseDelayID);
        if (!CanInteract(entity, (uid, storageComp), silent: silent))
            return;

        if (!UI.TryOpenUi(uid, StorageComponent.StorageUiKey.Key, entity))
            return;

        if (!silent)
        {
            Audio.PlayPredicted(storageComp.StorageOpenSound, uid, entity);

            if (useDelay != null)
                UseDelay.TryResetDelay((uid, useDelay), id: OpenUiUseDelayID);
        }
    }

    public virtual void UpdateUI(Entity<StorageComponent?> entity) {}

    private void AddTransferVerbs(EntityUid uid, StorageComponent component, GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var entities = component.Container.ContainedEntities;

        if (entities.Count == 0 || !CanInteract(args.User, (uid, component)))
            return;

        // if the target is storage, add a verb to transfer storage.
        if (TryComp(args.Target, out StorageComponent? targetStorage)
            && (!TryComp(args.Target, out LockComponent? targetLock) || !targetLock.Locked))
        {
            UtilityVerb verb = new()
            {
                Text = Loc.GetString("storage-component-transfer-verb"),
                IconEntity = GetNetEntity(args.Using),
                Act = () => TransferEntities(uid, args.Target, args.User, component, null, targetStorage, targetLock)
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
        if (args.Handled || !storageComp.ClickInsert || !CanInteract(args.User, (uid, storageComp), silent: false))
            return;

        var attemptEv = new StorageInteractUsingAttemptEvent();
        RaiseLocalEvent(uid, ref attemptEv);
        if (attemptEv.Cancelled)
            return;

        PlayerInsertHeldEntity((uid, storageComp), args.User);
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
        if (args.Handled || !args.Complex || !storageComp.OpenOnActivate || !CanInteract(args.User, (uid, storageComp)))
            return;

        // Toggle
        if (UI.IsUiOpen(uid, StorageComponent.StorageUiKey.Key, args.User))
        {
            UI.CloseUi(uid, StorageComponent.StorageUiKey.Key, args.User);
        }
        else
        {
            // Handle recursively opening nested storages.
            if (ContainerSystem.TryGetContainingContainer((args.Target, null, null), out var container) &&
                UI.IsUiOpen(container.Owner, StorageComponent.StorageUiKey.Key, args.User))
            {
                _nestedCheck = true;
                HideStorageWindow(container.Owner, args.User);
                OpenStorageUI(uid, args.User, storageComp, silent: true);
                _nestedCheck = false;
            }
            else
            {
                // If you need something more sophisticated for multi-UI you'll need to code some smarter
                // interactions.
                if (_openStorageLimit == 1)
                    UI.CloseUserUis<StorageComponent.StorageUiKey>(args.User);

                OpenStorageUI(uid, args.User, storageComp, silent: false);
            }
        }

        args.Handled = true;
    }

    protected virtual void HideStorageWindow(EntityUid uid, EntityUid actor)
    {
    }

    protected virtual void ShowStorageWindow(EntityUid uid, EntityUid actor)
    {
    }

    /// <summary>
    /// Specifically for storage implants.
    /// </summary>
    private void OnImplantActivate(EntityUid uid, StorageComponent storageComp, OpenStorageImplantEvent args)
    {
        if (args.Handled)
            return;

        var uiOpen = UI.IsUiOpen(uid, StorageComponent.StorageUiKey.Key, args.Performer);

        if (uiOpen)
            UI.CloseUi(uid, StorageComponent.StorageUiKey.Key, args.Performer);
        else
            OpenStorageUI(uid, args.Performer, storageComp, false);

        args.Handled = true;
    }

    /// <summary>
    /// Allows a user to pick up entities by clicking them, or pick up all entities in a certain radius
    /// around a click.
    /// </summary>
    /// <returns></returns>
    private void AfterInteract(EntityUid uid, StorageComponent storageComp, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || !UseDelay.TryResetDelay(uid, checkDelayed: true, id: QuickInsertUseDelayID))
            return;

        // Pick up all entities in a radius around the clicked location.
        // The last half of the if is because carpets exist and this is terrible
        if (storageComp.AreaInsert && (args.Target == null || !HasComp<ItemComponent>(args.Target.Value)))
        {
            _entList.Clear();
            _entSet.Clear();
            _entityLookupSystem.GetEntitiesInRange(args.ClickLocation, storageComp.AreaInsertRadius, _entSet, LookupFlags.Dynamic | LookupFlags.Sundries);
            var delay = 0f;

            foreach (var entity in _entSet)
            {
                if (entity == args.User
                    || !_itemQuery.TryGetComponent(entity, out var itemComp) // Need comp to get item size to get weight
                    || !_prototype.TryIndex(itemComp.Size, out var itemSize)
                    || !CanInsert(uid, entity, out _, storageComp, item: itemComp)
                    || !_interactionSystem.InRangeUnobstructed(args.User, entity))
                {
                    continue;
                }

                _entList.Add(entity);
                delay += itemSize.Weight * AreaInsertDelayPerItem;

                if (_entList.Count >= StorageComponent.AreaPickupLimit)
                    break;
            }

            //If there's only one then let's be generous
            if (_entList.Count >= 1)
            {
                var doAfterArgs = new DoAfterArgs(EntityManager, args.User, delay, new AreaPickupDoAfterEvent(GetNetEntityList(_entList)), uid, target: uid)
                {
                    BreakOnDamage = true,
                    BreakOnMove = true,
                    NeedHand = true,
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

            if (ContainerSystem.IsEntityInContainer(target)
                || target == args.User
                || !_itemQuery.HasComponent(target))
            {
                return;
            }

            if (TryComp(uid, out TransformComponent? transformOwner) && TryComp(target, out TransformComponent? transformEnt))
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
                    EntityManager.RaiseSharedEvent(new AnimateInsertingEntitiesEvent(GetNetEntity(uid),
                        new List<NetEntity> { GetNetEntity(target) },
                        new List<NetCoordinates> { GetNetCoordinates(position) },
                        new List<Angle> { transformOwner.LocalRotation }), args.User);
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

        if (!_xformQuery.TryGetComponent(uid, out var xform))
        {
            return;
        }

        var entCount = Math.Min(StorageComponent.AreaPickupLimit, args.Entities.Count);

        for (var i = 0; i < entCount; i++)
        {
            var entity = GetEntity(args.Entities[i]);

            // Check again, situation may have changed for some entities, but we'll still pick up any that are valid
            if (ContainerSystem.IsEntityInContainer(entity)
                || entity == args.Args.User
                || !_itemQuery.HasComponent(entity))
            {
                continue;
            }

            if (!_xformQuery.TryGetComponent(entity, out var targetXform) ||
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

            if (PlayerInsertEntityInWorld((uid, component), args.Args.User, entity, playSound: false))
            {
                successfullyInserted.Add(entity);
                successfullyInsertedPositions.Add(position);
                successfullyInsertedAngles.Add(angle);
            }
        }

        // If we picked up at least one thing, play a sound and do a cool animation!
        if (successfullyInserted.Count > 0)
        {
            Audio.PlayPredicted(component.StorageInsertSound, uid, args.User, _audioParams);
            EntityManager.RaiseSharedEvent(new AnimateInsertingEntitiesEvent(
                GetNetEntity(uid),
                GetNetEntityList(successfullyInserted),
                GetNetCoordinatesList(successfullyInsertedPositions),
                successfullyInsertedAngles), args.User);
        }

        args.Handled = true;
    }

    private void OnReclaimed(EntityUid uid, StorageComponent storageComp, GotReclaimedEvent args)
    {
        ContainerSystem.EmptyContainer(storageComp.Container, destination: args.ReclaimerCoordinates);
    }

    private void OnDestroy(EntityUid uid, StorageComponent storageComp, DestructionEventArgs args)
    {
        var coordinates = TransformSystem.GetMoverCoordinates(uid);

        // Being destroyed so need to recalculate.
        ContainerSystem.EmptyContainer(storageComp.Container, destination: coordinates);
    }

    /// <summary>
    ///     This function gets called when the user clicked on an item in the storage UI. This will either place the
    ///     item in the user's hand if it is currently empty, or interact with the item using the user's currently
    ///     held item.
    /// </summary>
    private void OnInteractWithItem(StorageInteractWithItemEvent msg, EntitySessionEventArgs args)
    {
        if (!ValidateInput(args, msg.StorageUid, msg.InteractedItemUid, out var player, out var storage, out var item))
            return;

        // If the user's active hand is empty, try pick up the item.
        if (player.Comp.ActiveHandEntity == null)
        {
            _adminLog.Add(
                LogType.Storage,
                LogImpact.Low,
                $"{ToPrettyString(player):player} is attempting to take {ToPrettyString(item):item} out of {ToPrettyString(storage):storage}");

            if (_sharedHandsSystem.TryPickupAnyHand(player, item, handsComp: player.Comp)
                && storage.Comp.StorageRemoveSound != null)
            {
                Audio.PlayPredicted(storage.Comp.StorageRemoveSound, storage, player, _audioParams);
            }

            return;
        }

        _adminLog.Add(
            LogType.Storage,
            LogImpact.Low,
            $"{ToPrettyString(player):player} is interacting with {ToPrettyString(item):item} while it is stored in {ToPrettyString(storage):storage} using {ToPrettyString(player.Comp.ActiveHandEntity):used}");

        // Else, interact using the held item
        if (_interactionSystem.InteractUsing(player,
                player.Comp.ActiveHandEntity.Value,
                item,
                Transform(item).Coordinates,
                checkCanInteract: false))
            return;

        var failedEv = new StorageInsertFailedEvent((storage, storage.Comp), (player, player.Comp));
        RaiseLocalEvent(storage, ref failedEv);
    }

    private void OnSetItemLocation(StorageSetItemLocationEvent msg, EntitySessionEventArgs args)
    {
        if (!ValidateInput(args, msg.StorageEnt, msg.ItemEnt, out var player, out var storage, out var item))
            return;

        _adminLog.Add(
            LogType.Storage,
            LogImpact.Low,
            $"{ToPrettyString(player):player} is updating the location of {ToPrettyString(item):item} within {ToPrettyString(storage):storage}");

        TrySetItemStorageLocation(item!, storage!, msg.Location);
    }

    private void OnStorageNested(OpenNestedStorageEvent msg, EntitySessionEventArgs args)
    {
        if (!NestedStorage)
            return;

        if (!TryGetEntity(msg.InteractedItemUid, out var itemEnt))
            return;

        _nestedCheck = true;

        var result = ValidateInput(args,
            msg.StorageUid,
            msg.InteractedItemUid,
            out var player,
            out var storage,
            out var item);

        if (!result)
        {
            _nestedCheck = false;
            return;
        }

        HideStorageWindow(storage.Owner, player.Owner);
        OpenStorageUI(item.Owner, player.Owner, silent: true);
        _nestedCheck = false;
    }

    private void OnStorageTransfer(StorageTransferItemEvent msg, EntitySessionEventArgs args)
    {
        if (!TryGetEntity(msg.ItemEnt, out var itemEnt))
            return;

        var localPlayer = args.SenderSession.AttachedEntity;

        if (!TryComp(localPlayer, out HandsComponent? handsComp) || !_sharedHandsSystem.TryPickup(localPlayer.Value, itemEnt.Value, handsComp: handsComp, animate: false))
            return;

        if (!ValidateInput(args, msg.StorageEnt, msg.ItemEnt, out var player, out var storage, out var item, held: true))
            return;

        _adminLog.Add(
            LogType.Storage,
            LogImpact.Low,
            $"{ToPrettyString(player):player} is inserting {ToPrettyString(item):item} into {ToPrettyString(storage):storage}");
        InsertAt(storage!, item!, msg.Location, out _, player, stackAutomatically: false);
    }

    private void OnInsertItemIntoLocation(StorageInsertItemIntoLocationEvent msg, EntitySessionEventArgs args)
    {
        if (!ValidateInput(args, msg.StorageEnt, msg.ItemEnt, out var player, out var storage, out var item, held: true))
            return;

        _adminLog.Add(
            LogType.Storage,
            LogImpact.Low,
            $"{ToPrettyString(player):player} is inserting {ToPrettyString(item):item} into {ToPrettyString(storage):storage}");
        InsertAt(storage!, item!, msg.Location, out _, player, stackAutomatically: false);
    }

    private void OnSaveItemLocation(StorageSaveItemLocationEvent msg, EntitySessionEventArgs args)
    {
        if (!ValidateInput(args, msg.Storage, msg.Item, out var player, out var storage, out var item))
            return;

        SaveItemLocation(storage!, item.Owner);
    }

    private void OnBoundUIOpen(Entity<StorageComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateAppearance((ent.Owner, ent.Comp, null));
    }

    private void OnBoundUIAttempt(BoundUserInterfaceMessageAttempt args)
    {
        if (args.UiKey is not StorageComponent.StorageUiKey.Key ||
            _openStorageLimit == -1 ||
            _nestedCheck ||
            args.Message is not OpenBoundInterfaceMessage)
            return;

        var uid = args.Target;
        var actor = args.Actor;
        var count = 0;

        if (_userQuery.TryComp(actor, out var userComp))
        {
            foreach (var (ui, keys) in userComp.OpenInterfaces)
            {
                if (ui == uid)
                    continue;

                foreach (var key in keys)
                {
                    if (key is not StorageComponent.StorageUiKey)
                        continue;

                    count++;

                    if (count >= _openStorageLimit)
                    {
                        args.Cancel();
                    }

                    break;
                }
            }
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
                ContainerSystem.Remove(args.Entity, args.Container, force: true);
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

        if (!CanInsert(uid, args.EntityUid, out var reason, component, ignoreStacks: true))
        {
#if DEBUG
            if (reason != null)
                CantFillReasons.Add(reason);
#endif

            args.Cancel();
        }
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

        var isOpen = UI.IsUiOpen(entity.Owner, StorageComponent.StorageUiKey.Key);

        _appearance.SetData(uid, StorageVisuals.StorageUsed, used, appearance);
        _appearance.SetData(uid, StorageVisuals.Capacity, capacity, appearance);
        _appearance.SetData(uid, StorageVisuals.Open, isOpen, appearance);
        _appearance.SetData(uid, SharedBagOpenVisuals.BagState, isOpen ? SharedBagState.Open : SharedBagState.Closed, appearance);

        // HideClosedStackVisuals true sets the StackVisuals.Hide to the open state of the storage.
        // This is for containers that only show their contents when open. (e.g. donut boxes)
        if (storage.HideStackVisualsWhenClosed)
            _appearance.SetData(uid, StackVisuals.Hide, !isOpen, appearance);
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

        Audio.PlayPredicted(sourceComp.StorageInsertSound, target, user, _audioParams);
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

        if (_whitelistSystem.IsWhitelistFail(storageComp.Whitelist, insertEnt) ||
            _whitelistSystem.IsBlacklistPass(storageComp.Blacklist, insertEnt))
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
        if (!ContainerSystem.CanInsert(insertEnt, storageComp.Container))
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
            if (!ContainerSystem.Insert(insertEnt, storageComp.Container))
                return false;

            if (playSound)
                Audio.PlayPredicted(storageComp.StorageInsertSound, uid, user, _audioParams);

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
            && !ContainerSystem.Insert(insertEnt, storageComp.Container)
            && toInsertCount == insertStack.Count)
        {
            // Failed to insert anything.
            return false;
        }

        if (playSound)
            Audio.PlayPredicted(storageComp.StorageInsertSound, uid, user, _audioParams);

        return true;
    }

    /// <summary>
    ///     Inserts an entity into storage from the player's active hand
    /// </summary>
    /// <param name="ent">The storage entity and component to insert into.</param>
    /// <param name="player">The player and hands component to insert the held entity from.</param>
    /// <returns>True if inserted, otherwise false.</returns>
    public bool PlayerInsertHeldEntity(Entity<StorageComponent?> ent, Entity<HandsComponent?> player)
    {
        if (!Resolve(ent.Owner, ref ent.Comp)
            || !Resolve(player.Owner, ref player.Comp)
            || player.Comp.ActiveHandEntity == null)
            return false;

        var toInsert = player.Comp.ActiveHandEntity;

        if (!CanInsert(ent, toInsert.Value, out var reason, ent.Comp))
        {
            _popupSystem.PopupClient(Loc.GetString(reason ?? "comp-storage-cant-insert"), ent, player);
            return false;
        }

        if (!_sharedHandsSystem.CanDrop(player, toInsert.Value, player.Comp))
        {
            _popupSystem.PopupClient(Loc.GetString("comp-storage-cant-drop", ("entity", toInsert.Value)), ent, player);
            return false;
        }

        return PlayerInsertEntityInWorld((ent, ent.Comp), player, toInsert.Value);
    }

    /// <summary>
    ///     Inserts an Entity (<paramref name="toInsert"/>) in the world into storage, informing <paramref name="player"/> if it fails.
    ///     <paramref name="toInsert"/> is *NOT* held, see <see cref="PlayerInsertHeldEntity(Entity{StorageComponent?},Entity{HandsComponent?})"/>.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="player">The player to insert an entity with</param>
    /// <param name="toInsert"></param>
    /// <returns>true if inserted, false otherwise</returns>
    public bool PlayerInsertEntityInWorld(Entity<StorageComponent?> uid, EntityUid player, EntityUid toInsert, bool playSound = true)
    {
        if (!Resolve(uid, ref uid.Comp) || !_interactionSystem.InRangeUnobstructed(player, uid.Owner))
            return false;

        if (!Insert(uid, toInsert, out _, user: player, uid.Comp, playSound: playSound))
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
        UpdateUI(storageEnt);
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
        UpdateUI((ent.Owner, ent.Comp));
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
        {
            if (_prototype.TryIndex(uid.Comp.MaxItemSize.Value, out var proto))
                return proto;

            Log.Error($"{ToPrettyString(uid.Owner)} tried to get invalid item size prototype: {uid.Comp.MaxItemSize.Value}. Stack trace:\\n{Environment.StackTrace}");
        }

        if (!_itemQuery.TryGetComponent(uid, out var item))
            return _defaultStorageMaxItemSize;

        // if there is no max item size specified, the value used
        // is one below the item size of the storage entity.
        return _nextSmallest[item.Size];
    }

    /// <summary>
    /// Checks if a storage's UI is open by anyone when locked, and closes it.
    /// </summary>
    private void OnLockToggled(EntityUid uid, StorageComponent component, ref LockToggledEvent args)
    {
        if (!args.Locked)
            return;

        // Gets everyone looking at the UI
        foreach (var actor in UI.GetActors(uid, StorageComponent.StorageUiKey.Key).ToList())
        {
            if (!CanInteract(actor, (uid, component)))
                UI.CloseUi(uid, StorageComponent.StorageUiKey.Key, actor);
        }
    }

    private void OnStackCountChanged(EntityUid uid, MetaDataComponent component, StackCountChangedEvent args)
    {
        if (ContainerSystem.TryGetContainingContainer((uid, null, component), out var container) &&
            container.ID == StorageComponent.ContainerId)
        {
            UpdateAppearance(container.Owner);
            UpdateUI(container.Owner);
        }
    }

    private void HandleOpenBackpack(ICommonSession? session)
    {
        HandleToggleSlotUI(session, "back");
    }

    private void HandleOpenBelt(ICommonSession? session)
    {
        HandleToggleSlotUI(session, "belt");
    }

    private void HandleToggleSlotUI(ICommonSession? session, string slot)
    {
        if (session is not { } playerSession)
            return;

        if (playerSession.AttachedEntity is not { Valid: true } playerEnt || !Exists(playerEnt))
            return;

        if (!_inventory.TryGetSlotEntity(playerEnt, slot, out var storageEnt))
            return;

        if (!ActionBlocker.CanInteract(playerEnt, storageEnt))
            return;

        if (!UI.IsUiOpen(storageEnt.Value, StorageComponent.StorageUiKey.Key, playerEnt))
        {
            OpenStorageUI(storageEnt.Value, playerEnt, silent: false);
        }
        else
        {
            UI.CloseUi(storageEnt.Value, StorageComponent.StorageUiKey.Key, playerEnt);
        }
    }

    protected void ClearCantFillReasons()
    {
#if DEBUG
        CantFillReasons.Clear();
#endif
    }

    private bool CanInteract(EntityUid user, Entity<StorageComponent> storage, bool canInteract = true, bool silent = true)
    {
        if (HasComp<BypassInteractionChecksComponent>(user))
            return true;

        if (!canInteract)
            return false;

        var ev = new StorageInteractAttemptEvent(silent);
        RaiseLocalEvent(storage, ref ev);

        return !ev.Cancelled;
    }

    /// <summary>
    /// Plays a clientside pickup animation for the specified uid.
    /// </summary>
    public abstract void PlayPickupAnimation(EntityUid uid, EntityCoordinates initialCoordinates,
        EntityCoordinates finalCoordinates, Angle initialRotation, EntityUid? user = null);

    private bool ValidateInput(
        EntitySessionEventArgs args,
        NetEntity netStorage,
        out Entity<HandsComponent> player,
        out Entity<StorageComponent> storage)
    {
        player = default;
        storage = default;

        if (args.SenderSession.AttachedEntity is not { } playerUid)
            return false;

        if (!TryComp(playerUid, out HandsComponent? hands) || hands.Count == 0)
            return false;

        if (!TryGetEntity(netStorage, out var storageUid))
            return false;

        if (!TryComp(storageUid, out StorageComponent? storageComp))
            return false;

        // TODO STORAGE use BUI events
        // This would automatically validate that the UI is open & that the user can interact.
        // However, we still need to manually validate that items being used are in the users hands or in the storage.
        if (!UI.IsUiOpen(storageUid.Value, StorageComponent.StorageUiKey.Key, playerUid))
            return false;

        if (!ActionBlocker.CanInteract(playerUid, storageUid))
            return false;

        player = new(playerUid, hands);
        storage = new(storageUid.Value, storageComp);
        return true;
    }

    private bool ValidateInput(EntitySessionEventArgs args,
        NetEntity netStorage,
        NetEntity netItem,
        out Entity<HandsComponent> player,
        out Entity<StorageComponent> storage,
        out Entity<ItemComponent> item,
        bool held = false)
    {
        item = default!;
        if (!ValidateInput(args, netStorage, out player, out storage))
            return false;

        if (!TryGetEntity(netItem, out var itemUid))
            return false;

        if (held)
        {
            if (!_sharedHandsSystem.IsHolding(player, itemUid, out _))
                return false;
        }
        else
        {
            if (!storage.Comp.Container.Contains(itemUid.Value))
                return false;

            DebugTools.Assert(storage.Comp.StoredItems.ContainsKey(itemUid.Value));
        }

        if (!TryComp(itemUid, out ItemComponent? itemComp))
            return false;

        if (!ActionBlocker.CanInteract(player, itemUid))
            return false;

        item = new(itemUid.Value, itemComp);
        return true;
    }

    [Serializable, NetSerializable]
    protected sealed class StorageComponentState : ComponentState
    {
        public Dictionary<NetEntity, ItemStorageLocation> StoredItems = new();

        public Dictionary<string, List<ItemStorageLocation>> SavedLocations = new();

        public List<Box2i> Grid = new();

        public ProtoId<ItemSizePrototype>? MaxItemSize;

        public EntityWhitelist? Whitelist;

        public EntityWhitelist? Blacklist;
    }
}
