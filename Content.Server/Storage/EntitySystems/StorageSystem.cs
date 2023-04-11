using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Ghost.Components;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Server.Storage.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration;
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
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Content.Shared.Storage.SharedStorageComponent;

namespace Content.Server.Storage.EntitySystems
{
    [UsedImplicitly]
    public sealed partial class StorageSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IAdminManager _admin = default!;
        [Dependency] private readonly ContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
        [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
        [Dependency] private readonly InteractionSystem _interactionSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedHandsSystem _sharedHandsSystem = default!;
        [Dependency] private readonly SharedInteractionSystem _sharedInteractionSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly UseDelaySystem _useDelay = default!;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ServerStorageComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<ServerStorageComponent, GetVerbsEvent<ActivationVerb>>(AddOpenUiVerb);
            SubscribeLocalEvent<ServerStorageComponent, GetVerbsEvent<UtilityVerb>>(AddTransferVerbs);
            SubscribeLocalEvent<ServerStorageComponent, InteractUsingEvent>(OnInteractUsing, after: new []{ typeof(ItemSlotsSystem)} );
            SubscribeLocalEvent<ServerStorageComponent, ActivateInWorldEvent>(OnActivate);
            SubscribeLocalEvent<ServerStorageComponent, OpenStorageImplantEvent>(OnImplantActivate);
            SubscribeLocalEvent<ServerStorageComponent, AfterInteractEvent>(AfterInteract);
            SubscribeLocalEvent<ServerStorageComponent, DestructionEventArgs>(OnDestroy);
            SubscribeLocalEvent<ServerStorageComponent, StorageInteractWithItemEvent>(OnInteractWithItem);
            SubscribeLocalEvent<ServerStorageComponent, StorageInsertItemMessage>(OnInsertItemMessage);
            SubscribeLocalEvent<ServerStorageComponent, BoundUIOpenedEvent>(OnBoundUIOpen);
            SubscribeLocalEvent<ServerStorageComponent, BoundUIClosedEvent>(OnBoundUIClosed);
            SubscribeLocalEvent<ServerStorageComponent, EntRemovedFromContainerMessage>(OnStorageItemRemoved);

            SubscribeLocalEvent<ServerStorageComponent, AreaPickupDoAfterEvent>(OnDoAfter);

            SubscribeLocalEvent<StorageFillComponent, MapInitEvent>(OnStorageFillMapInit);
        }

        private void OnComponentInit(EntityUid uid, ServerStorageComponent storageComp, ComponentInit args)
        {
            base.Initialize();

            // ReSharper disable once StringLiteralTypo
            storageComp.Storage = _containerSystem.EnsureContainer<Container>(uid, "storagebase");
            storageComp.Storage.OccludesLight = storageComp.OccludesLight;
            UpdateStorageVisualization(uid, storageComp);
            RecalculateStorageUsed(storageComp);
            UpdateStorageUI(uid, storageComp);
        }

        private void AddOpenUiVerb(EntityUid uid, ServerStorageComponent component, GetVerbsEvent<ActivationVerb> args)
        {
            bool silent = false;
            if (!args.CanAccess || !args.CanInteract || TryComp<LockComponent>(uid, out var lockComponent) && lockComponent.Locked)
            {
                // we allow admins to open the storage anyways
                if (!_admin.HasAdminFlag(args.User, AdminFlags.Admin))
                    return;

                silent = true;
            }

            silent |= HasComp<GhostComponent>(args.User);

            // Get the session for the user
            if (!TryComp<ActorComponent>(args.User, out var actor))
                return;

            // Does this player currently have the storage UI open?
            bool uiOpen = _uiSystem.SessionHasOpenUi(uid, StorageUiKey.Key, actor.PlayerSession);

            ActivationVerb verb = new()
            {
                Act = () => OpenStorageUI(uid, args.User, component, silent)
            };
            if (uiOpen)
            {
                verb.Text = Loc.GetString("verb-common-close-ui");
                verb.Icon = new SpriteSpecifier.Texture(
                    new ResourcePath("/Textures/Interface/VerbIcons/close.svg.192dpi.png"));
            }
            else
            {
                verb.Text = Loc.GetString("verb-common-open-ui");
                verb.Icon = new SpriteSpecifier.Texture(
                    new ResourcePath("/Textures/Interface/VerbIcons/open.svg.192dpi.png"));
            }
            args.Verbs.Add(verb);
        }

        private void AddTransferVerbs(EntityUid uid, ServerStorageComponent component, GetVerbsEvent<UtilityVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            var entities = component.Storage?.ContainedEntities;
            if (entities == null || entities.Count == 0 || TryComp(uid, out LockComponent? lockComponent) && lockComponent.Locked)
                return;

            // if the target is storage, add a verb to transfer storage.
            if (TryComp(args.Target, out ServerStorageComponent? targetStorage)
                && (!TryComp(uid, out LockComponent? targetLock) || !targetLock.Locked))
            {
                UtilityVerb verb = new()
                {
                    Text = Loc.GetString("storage-component-transfer-verb"),
                    IconEntity = args.Using,
                    Act = () => TransferEntities(uid, args.Target, component, lockComponent, targetStorage, targetLock)
                };

                args.Verbs.Add(verb);
            }
        }

        /// <summary>
        /// Inserts storable entities into this storage container if possible, otherwise return to the hand of the user
        /// </summary>
        /// <returns>true if inserted, false otherwise</returns>
        private void OnInteractUsing(EntityUid uid, ServerStorageComponent storageComp, InteractUsingEvent args)
        {
            if (args.Handled || !storageComp.ClickInsert || TryComp(uid, out LockComponent? lockComponent) && lockComponent.Locked)
                return;

            Logger.DebugS(storageComp.LoggerName, $"Storage (UID {uid}) attacked by user (UID {args.User}) with entity (UID {args.Used}).");

            if (HasComp<PlaceableSurfaceComponent>(uid))
                return;

            if (PlayerInsertHeldEntity(uid, args.User, storageComp))
                args.Handled = true;
        }

        /// <summary>
        /// Sends a message to open the storage UI
        /// </summary>
        /// <returns></returns>
        private void OnActivate(EntityUid uid, ServerStorageComponent storageComp, ActivateInWorldEvent args)
        {
            if (args.Handled || _combatMode.IsInCombatMode(args.User) || TryComp(uid, out LockComponent? lockComponent) && lockComponent.Locked)
                return;

            OpenStorageUI(uid, args.User, storageComp);
        }

        /// <summary>
        /// Specifically for storage implants.
        /// </summary>
        private void OnImplantActivate(EntityUid uid, ServerStorageComponent storageComp, OpenStorageImplantEvent args)
        {
            if (args.Handled || !TryComp<TransformComponent>(uid, out var xform))
                return;

            OpenStorageUI(uid, xform.ParentUid, storageComp);
        }

        /// <summary>
        /// Allows a user to pick up entities by clicking them, or pick up all entities in a certain radius
        /// around a click.
        /// </summary>
        /// <returns></returns>
        private async void AfterInteract(EntityUid uid, ServerStorageComponent storageComp, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;

            // Pick up all entities in a radius around the clicked location.
            // The last half of the if is because carpets exist and this is terrible
            if (storageComp.AreaInsert && (args.Target == null || !HasComp<ItemComponent>(args.Target.Value)))
            {
                var validStorables = new List<EntityUid>();
                var itemQuery = GetEntityQuery<ItemComponent>();

                foreach (var entity in _entityLookupSystem.GetEntitiesInRange(args.ClickLocation, storageComp.AreaInsertRadius, LookupFlags.Dynamic | LookupFlags.Sundries))
                {
                    if (entity == args.User
                        || !itemQuery.HasComponent(entity)
                        || !CanInsert(uid, entity, out _, storageComp)
                        || !_interactionSystem.InRangeUnobstructed(args.User, entity))
                        continue;

                    validStorables.Add(entity);
                }

                //If there's only one then let's be generous
                if (validStorables.Count > 1)
                {
                    var doAfterArgs = new DoAfterArgs(args.User, 0.2f * validStorables.Count, new AreaPickupDoAfterEvent(validStorables), uid, target: uid)
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
                if (args.Target is not {Valid: true} target)
                    return;

                if (_containerSystem.IsEntityInContainer(target)
                    || target == args.User
                    || !HasComp<ItemComponent>(target))
                    return;

                if (TryComp<TransformComponent>(uid, out var transformOwner) && TryComp<TransformComponent>(target, out var transformEnt))
                {
                    var parent = transformOwner.ParentUid;

                    var position = EntityCoordinates.FromMap(
                        parent.IsValid() ? parent : uid,
                        transformEnt.MapPosition);

                    if (PlayerInsertEntityInWorld(uid, args.User, target, storageComp))
                    {
                        RaiseNetworkEvent(new AnimateInsertingEntitiesEvent(uid,
                            new List<EntityUid> { target },
                            new List<EntityCoordinates> { position }));
                    }
                }
            }
        }

        private void OnDoAfter(EntityUid uid, ServerStorageComponent component, AreaPickupDoAfterEvent args)
        {
            if (args.Handled || args.Cancelled)
                return;

            var successfullyInserted = new List<EntityUid>();
            var successfullyInsertedPositions = new List<EntityCoordinates>();
            var itemQuery = GetEntityQuery<ItemComponent>();
            var xformQuery = GetEntityQuery<TransformComponent>();
            xformQuery.TryGetComponent(uid, out var xform);

            foreach (var entity in args.Entities)
            {
                // Check again, situation may have changed for some entities, but we'll still pick up any that are valid
                if (_containerSystem.IsEntityInContainer(entity)
                    || entity == args.Args.User
                    || !itemQuery.HasComponent(entity))
                    continue;

                if (xform == null ||
                    !xformQuery.TryGetComponent(entity, out var targetXform) ||
                    targetXform.MapID != xform.MapID)
                {
                    continue;
                }

                var position = EntityCoordinates.FromMap(
                    xform.ParentUid.IsValid() ? xform.ParentUid : uid,
                    new MapCoordinates(_transform.GetWorldPosition(targetXform, xformQuery),
                        targetXform.MapID), EntityManager);

                if (PlayerInsertEntityInWorld(uid, args.Args.User, entity, component))
                {
                    successfullyInserted.Add(entity);
                    successfullyInsertedPositions.Add(position);
                }
            }

            // If we picked up atleast one thing, play a sound and do a cool animation!
            if (successfullyInserted.Count > 0)
            {
                _audio.PlayPvs(component.StorageInsertSound, uid);
                RaiseNetworkEvent(new AnimateInsertingEntitiesEvent(uid, successfullyInserted, successfullyInsertedPositions));
            }

            args.Handled = true;
        }

        private void OnDestroy(EntityUid uid, ServerStorageComponent storageComp, DestructionEventArgs args)
        {
            var storedEntities = storageComp.StoredEntities?.ToList();

            if (storedEntities == null)
                return;

            foreach (var entity in storedEntities)
            {
                RemoveAndDrop(uid, entity, storageComp);
            }
        }

        /// <summary>
        ///     This function gets called when the user clicked on an item in the storage UI. This will either place the
        ///     item in the user's hand if it is currently empty, or interact with the item using the user's currently
        ///     held item.
        /// </summary>
        private void OnInteractWithItem(EntityUid uid, ServerStorageComponent storageComp, StorageInteractWithItemEvent args)
        {
            // TODO move this to shared for prediction.
            if (args.Session.AttachedEntity is not EntityUid player)
                return;

            if (!Exists(args.InteractedItemUID))
            {
                Logger.Error($"Player {args.Session} interacted with non-existent item {args.InteractedItemUID} stored in {ToPrettyString(uid)}");
                return;
            }

            if (!_actionBlockerSystem.CanInteract(player, args.InteractedItemUID) || storageComp.Storage == null || !storageComp.Storage.Contains(args.InteractedItemUID))
                return;

            // Does the player have hands?
            if (!TryComp(player, out HandsComponent? hands) || hands.Count == 0)
                return;

            // If the user's active hand is empty, try pick up the item.
            if (hands.ActiveHandEntity == null)
            {
                if (_sharedHandsSystem.TryPickupAnyHand(player, args.InteractedItemUID, handsComp: hands)
                    && storageComp.StorageRemoveSound != null)
                    _audio.Play(storageComp.StorageRemoveSound, Filter.Pvs(uid, entityManager: EntityManager), uid, true, AudioParams.Default);
                return;
            }

            // Else, interact using the held item
            _interactionSystem.InteractUsing(player, hands.ActiveHandEntity.Value, args.InteractedItemUID, Transform(args.InteractedItemUID).Coordinates, checkCanInteract: false);
        }

        private void OnInsertItemMessage(EntityUid uid, ServerStorageComponent storageComp, StorageInsertItemMessage args)
        {
            // TODO move this to shared for prediction.
            if (args.Session.AttachedEntity == null)
                return;

            PlayerInsertHeldEntity(uid, args.Session.AttachedEntity.Value, storageComp);
        }

        private void OnBoundUIOpen(EntityUid uid, ServerStorageComponent storageComp, BoundUIOpenedEvent args)
        {
            if (!storageComp.IsOpen)
            {
                storageComp.IsOpen = true;
                UpdateStorageVisualization(uid, storageComp);
            }
        }

        private void OnBoundUIClosed(EntityUid uid, ServerStorageComponent storageComp, BoundUIClosedEvent args)
        {
            if (TryComp<ActorComponent>(args.Session.AttachedEntity, out var actor) && actor?.PlayerSession != null)
                CloseNestedInterfaces(uid, actor.PlayerSession, storageComp);

            // If UI is closed for everyone
            if (!_uiSystem.IsUiOpen(uid, args.UiKey))
            {
                storageComp.IsOpen = false;
                UpdateStorageVisualization(uid, storageComp);

                if (storageComp.StorageCloseSound is not null)
                    _audio.Play(storageComp.StorageCloseSound, Filter.Pvs(uid, entityManager: EntityManager), uid, true, storageComp.StorageCloseSound.Params);
            }
        }

        private void OnStorageItemRemoved(EntityUid uid, ServerStorageComponent storageComp, EntRemovedFromContainerMessage args)
        {
            RecalculateStorageUsed(storageComp);
            UpdateStorageUI(uid, storageComp);
        }

        private void UpdateStorageVisualization(EntityUid uid, ServerStorageComponent storageComp)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            _appearance.SetData(uid, StorageVisuals.Open, storageComp.IsOpen, appearance);
            _appearance.SetData(uid, SharedBagOpenVisuals.BagState, storageComp.IsOpen ? SharedBagState.Open : SharedBagState.Closed);

            if (HasComp<ItemCounterComponent>(uid))
                _appearance.SetData(uid, StackVisuals.Hide, !storageComp.IsOpen);
        }

        private void RecalculateStorageUsed(ServerStorageComponent storageComp)
        {
            storageComp.StorageUsed = 0;
            storageComp.SizeCache.Clear();

            if (storageComp.Storage == null)
                return;

            var itemQuery = GetEntityQuery<ItemComponent>();

            foreach (var entity in storageComp.Storage.ContainedEntities)
            {
                if (!itemQuery.TryGetComponent(entity, out var itemComp))
                    continue;

                storageComp.StorageUsed += itemComp.Size;
                storageComp.SizeCache.Add(entity, itemComp.Size);
            }
        }

        /// <summary>
        ///     Move entities from one storage to another.
        /// </summary>
        public void TransferEntities(EntityUid source, EntityUid target,
            ServerStorageComponent? sourceComp = null, LockComponent? sourceLock = null,
            ServerStorageComponent? targetComp = null, LockComponent? targetLock = null)
        {
            if (!Resolve(source, ref sourceComp) || !Resolve(target, ref targetComp))
                return;

            var entities = sourceComp.Storage?.ContainedEntities;
            if (entities == null || entities.Count == 0)
                return;

            if (Resolve(source, ref sourceLock, false) && sourceLock.Locked
                || Resolve(target, ref targetLock, false) && targetLock.Locked)
                return;

            foreach (var entity in entities.ToList())
            {
                Insert(target, entity, targetComp);
            }
            RecalculateStorageUsed(sourceComp);
            UpdateStorageUI(source, sourceComp);
        }

        /// <summary>
        ///     Verifies if an entity can be stored and if it fits
        /// </summary>
        /// <param name="entity">The entity to check</param>
        /// <param name="reason">If returning false, the reason displayed to the player</param>
        /// <returns>true if it can be inserted, false otherwise</returns>
        public bool CanInsert(EntityUid uid, EntityUid insertEnt, out string? reason, ServerStorageComponent? storageComp = null)
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

            if (TryComp(insertEnt, out ServerStorageComponent? storage) &&
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
        public bool Insert(EntityUid uid, EntityUid insertEnt, ServerStorageComponent? storageComp = null, bool playSound = true)
        {
            if (!Resolve(uid, ref storageComp) || !CanInsert(uid, insertEnt, out _, storageComp) || storageComp.Storage?.Insert(insertEnt) == false)
                return false;

            if (playSound && storageComp.StorageInsertSound is not null)
                _audio.PlayPvs(storageComp.StorageInsertSound, uid);

            RecalculateStorageUsed(storageComp);
            UpdateStorageUI(uid, storageComp);
            return true;
        }

        // REMOVE: remove and drop on the ground
        public bool RemoveAndDrop(EntityUid uid, EntityUid removeEnt, ServerStorageComponent? storageComp = null)
        {
            if (!Resolve(uid, ref storageComp))
                return false;

            var itemRemoved = storageComp.Storage?.Remove(removeEnt) == true;
            if (itemRemoved)
                RecalculateStorageUsed(storageComp);

            return itemRemoved;
        }

        /// <summary>
        ///     Inserts an entity into storage from the player's active hand
        /// </summary>
        /// <param name="player">The player to insert an entity from</param>
        /// <returns>true if inserted, false otherwise</returns>
        public bool PlayerInsertHeldEntity(EntityUid uid, EntityUid player, ServerStorageComponent? storageComp = null)
        {
            if (!Resolve(uid, ref storageComp) || !TryComp(player, out HandsComponent? hands) || hands.ActiveHandEntity == null)
                return false;

            var toInsert = hands.ActiveHandEntity;

            if (!CanInsert(uid, toInsert.Value, out var reason, storageComp) || !_sharedHandsSystem.TryDrop(player, toInsert.Value, handsComp: hands))
            {
                Popup(uid, player, reason ?? "comp-storage-cant-insert", storageComp);
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
        public bool PlayerInsertEntityInWorld(EntityUid uid, EntityUid player, EntityUid toInsert, ServerStorageComponent? storageComp = null)
        {
            if (!Resolve(uid, ref storageComp) || !_sharedInteractionSystem.InRangeUnobstructed(player, uid, popup: storageComp.ShowPopup))
                return false;

            if (!Insert(uid, toInsert, storageComp))
            {
                Popup(uid, player, "comp-storage-cant-insert", storageComp);
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Opens the storage UI for an entity
        /// </summary>
        /// <param name="entity">The entity to open the UI for</param>
        public void OpenStorageUI(EntityUid uid, EntityUid entity, ServerStorageComponent? storageComp = null, bool silent = false)
        {
            if (!Resolve(uid, ref storageComp) || !TryComp(entity, out ActorComponent? player))
                return;

            // prevent spamming bag open / honkerton honk sound
            silent |= TryComp<UseDelayComponent>(uid, out var useDelay) && _useDelay.ActiveDelay(uid, useDelay);
            if (!silent)
            {
                _audio.PlayPvs(storageComp.StorageOpenSound, uid);
                if (useDelay != null)
                    _useDelay.BeginDelay(uid, useDelay);
            }

            Logger.DebugS(storageComp.LoggerName, $"Storage (UID {uid}) \"used\" by player session (UID {player.PlayerSession.AttachedEntity}).");

            var bui = _uiSystem.GetUiOrNull(uid, StorageUiKey.Key);
            if (bui != null)
                _uiSystem.OpenUi(bui, player.PlayerSession);
        }

        /// <summary>
        ///     If the user has nested-UIs open (e.g., PDA UI open when pda is in a backpack), close them.
        /// </summary>
        /// <param name="session"></param>
        public void CloseNestedInterfaces(EntityUid uid, IPlayerSession session, ServerStorageComponent? storageComp = null)
        {
            if (!Resolve(uid, ref storageComp) || storageComp.StoredEntities == null)
                return;

            // for each containing thing
            // if it has a storage comp
            // ensure unsubscribe from session
            // if it has a ui component
            // close ui
            foreach (var entity in storageComp.StoredEntities)
            {
                if (TryComp(entity, out ServerStorageComponent? storedStorageComp))
                    DebugTools.Assert(storedStorageComp != storageComp, $"Storage component contains itself!? Entity: {uid}");

                if (!TryComp(entity, out ServerUserInterfaceComponent? ui))
                    continue;

                foreach (var bui in ui.Interfaces.Values)
                {
                    _uiSystem.TryClose(entity, bui.UiKey, session, ui);
                }
            }
        }

        private void UpdateStorageUI(EntityUid uid, ServerStorageComponent storageComp)
        {
            if (storageComp.Storage == null)
                return;

            var state = new StorageBoundUserInterfaceState((List<EntityUid>) storageComp.Storage.ContainedEntities, storageComp.StorageUsed, storageComp.StorageCapacityMax);

            var bui = _uiSystem.GetUiOrNull(uid, StorageUiKey.Key);
            if (bui != null)
                _uiSystem.SetUiState(bui, state);
        }

        private void Popup(EntityUid uid, EntityUid player, string message, ServerStorageComponent storageComp)
        {
            if (!storageComp.ShowPopup)
                return;

            _popupSystem.PopupEntity(Loc.GetString(message), player, player);
        }
    }
}
