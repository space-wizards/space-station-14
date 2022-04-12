using System.Linq;
using Content.Server.Disposal.Unit.Components;
using Content.Server.Disposal.Unit.EntitySystems;
using Content.Server.Hands.Components;
using Content.Server.Storage.Components;
using Content.Shared.Interaction;
using Content.Shared.Movement;
using Content.Shared.Storage;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Threading;
using Content.Server.DoAfter;
using Content.Server.Interaction;
using Content.Shared.Acts;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Storage.Components;
using Robust.Shared.Audio;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Server.Containers;

using static Content.Shared.Storage.SharedStorageComponent;

namespace Content.Server.Storage.EntitySystems
{
    [UsedImplicitly]
    public sealed partial class StorageSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly DisposalUnitSystem _disposalSystem = default!;
        [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
        [Dependency] private readonly ContainerSystem _containerSystem = default!;
        [Dependency] private readonly InteractionSystem _interactionSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SharedHandsSystem _sharedHandsSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EntityStorageComponent, GetVerbsEvent<InteractionVerb>>(AddToggleOpenVerb);
            SubscribeLocalEvent<EntityStorageComponent, RelayMovementEntityEvent>(OnRelayMovement);
            SubscribeLocalEvent<ServerStorageComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<ServerStorageComponent, GetVerbsEvent<ActivationVerb>>(AddOpenUiVerb);
            SubscribeLocalEvent<ServerStorageComponent, GetVerbsEvent<UtilityVerb>>(AddTransferVerbs);
            SubscribeLocalEvent<ServerStorageComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<ServerStorageComponent, ActivateInWorldEvent>(OnActivate);
            SubscribeLocalEvent<ServerStorageComponent, AfterInteractEvent>(AfterInteract);
            SubscribeLocalEvent<ServerStorageComponent, DestructionEventArgs>(OnDestroy);
            SubscribeLocalEvent<ServerStorageComponent, StorageInteractItemMessage>(OnUIInteractMessage);

            SubscribeLocalEvent<StorageFillComponent, MapInitEvent>(OnStorageFillMapInit);
        }

        private void OnRelayMovement(EntityUid uid, EntityStorageComponent component, RelayMovementEntityEvent args)
        {
            if (!EntityManager.HasComponent<HandsComponent>(args.Entity))
                return;

            if (_gameTiming.CurTime <
                component.LastInternalOpenAttempt + EntityStorageComponent.InternalOpenAttemptDelay)
            {
                return;
            }

            component.LastInternalOpenAttempt = _gameTiming.CurTime;
            component.TryOpenStorage(args.Entity);
        }


        private void AddToggleOpenVerb(EntityUid uid, EntityStorageComponent component, GetVerbsEvent<InteractionVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (!component.CanOpen(args.User, silent: true))
                return;

            InteractionVerb verb = new();
            if (component.Open)
            {
                verb.Text = Loc.GetString("verb-common-close");
                verb.IconTexture = "/Textures/Interface/VerbIcons/close.svg.192dpi.png";
            }
            else
            {
                verb.Text = Loc.GetString("verb-common-open");
                verb.IconTexture = "/Textures/Interface/VerbIcons/open.svg.192dpi.png";
            }
            verb.Act = () => component.ToggleOpen(args.User);
            args.Verbs.Add(verb);
        }

        private void AddOpenUiVerb(EntityUid uid, ServerStorageComponent component, GetVerbsEvent<ActivationVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            if (EntityManager.TryGetComponent(uid, out LockComponent? lockComponent) && lockComponent.Locked)
                return;

            // Get the session for the user
            var session = EntityManager.GetComponentOrNull<ActorComponent>(args.User)?.PlayerSession;
            if (session == null)
                return;

            // Does this player currently have the storage UI open?
            // var uiOpen = component.SubscribedSessions.Contains(session);
            var uiOpen = false;
            // I'll come back to this one wtf

            ActivationVerb verb = new();
            verb.Act = () => OpenStorageUI(uid, args.User, component);
            if (uiOpen)
            {
                verb.Text = Loc.GetString("verb-common-close-ui");
                verb.IconTexture = "/Textures/Interface/VerbIcons/close.svg.192dpi.png";
            }
            else
            {
                verb.Text = Loc.GetString("verb-common-open-ui");
                verb.IconTexture = "/Textures/Interface/VerbIcons/open.svg.192dpi.png";
            }
            args.Verbs.Add(verb);
        }

        private void AddTransferVerbs(EntityUid uid, ServerStorageComponent component, GetVerbsEvent<UtilityVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract)
                return;

            var entities = component.Storage?.ContainedEntities;
            if (entities == null || entities.Count == 0)
                return;

            if (TryComp(uid, out LockComponent? lockComponent) && lockComponent.Locked)
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

            // if the target is a disposal unit, add a verb to transfer storage into the unit (e.g., empty a trash bag).
            if (!TryComp(args.Target, out DisposalUnitComponent? disposal))
                return;

            UtilityVerb dispose = new()
            {
                Text = Loc.GetString("storage-component-dispose-verb"),
                IconEntity = args.Using,
                Act = () => DisposeEntities(args.User, uid, args.Target, component, lockComponent, disposal)
            };

            args.Verbs.Add(dispose);
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
        }

        /// <summary>
        ///     Move entities from storage into a disposal unit.
        /// </summary>
        public void DisposeEntities(EntityUid user, EntityUid source, EntityUid target,
            ServerStorageComponent? sourceComp = null, LockComponent? sourceLock = null,
            DisposalUnitComponent? disposalComp = null)
        {
            if (!Resolve(source, ref sourceComp) || !Resolve(target, ref disposalComp))
                return;

            var entities = sourceComp.Storage?.ContainedEntities;
            if (entities == null || entities.Count == 0)
                return;

            if (Resolve(source, ref sourceLock, false) && sourceLock.Locked)
                return;

            foreach (var entity in entities.ToList())
            {
                if (_disposalSystem.CanInsert(disposalComp, entity)
                    && disposalComp.Container.Insert(entity))
                {
                    _disposalSystem.AfterInsert(disposalComp, entity);
                }
            }
        }

        private void UpdateStorageVisualization(EntityUid uid, ServerStorageComponent storageComp)
        {
            if (!TryComp<AppearanceComponent>(uid, out var appearance))
                return;

            // bool open = storageComp.SubscribedSessions.Count != 0;
            // maybe i need to check into this one again
            // wtf uses this anyhow
            bool open = false;

            appearance.SetData(StorageVisuals.Open, open);
            appearance.SetData(SharedBagOpenVisuals.BagState, open ? SharedBagState.Open : SharedBagState.Closed);

            if (HasComp<ItemCounterComponent>(uid))
                appearance.SetData(StackVisuals.Hide, !open);
        }

        private void EnsureInitialCalculated(EntityUid uid, ServerStorageComponent storageComp)
        {
            if (storageComp.StorageInitialCalculated)
                return;

            RecalculateStorageUsed(uid, storageComp);

            storageComp.StorageInitialCalculated = true;
        }

        private void RecalculateStorageUsed(EntityUid uid, ServerStorageComponent storageComp)
        {
            storageComp.StorageUsed = 0;
            storageComp.SizeCache.Clear();

            if (storageComp.Storage == null)
                return;
            Logger.Debug("Ran storage used");

            foreach (var entity in storageComp.Storage.ContainedEntities)
            {
                // not sure of the repercussions of this, figure out later
                if (!TryComp<SharedItemComponent>(entity, out var itemComp))
                    continue;
                Logger.Debug("Item found");
                storageComp.StorageUsed += itemComp.Size;
                storageComp.SizeCache.Add(entity, itemComp.Size);
            }
        }

        /// <summary>
        ///     Verifies if an entity can be stored and if it fits
        /// </summary>
        /// <param name="entity">The entity to check</param>
        /// <returns>true if it can be inserted, false otherwise</returns>
        public bool CanInsert(EntityUid uid, EntityUid insertEnt, ServerStorageComponent storageComp)
        {
            EnsureInitialCalculated(uid, storageComp);

            if (TryComp<ServerStorageComponent>(insertEnt, out var storage) &&
                storage.StorageCapacityMax >= storageComp.StorageCapacityMax)
                return false;

            if (TryComp<SharedItemComponent>(insertEnt, out var itemComp) &&
                itemComp.Size > storageComp.StorageCapacityMax - storageComp.StorageUsed)
                return false;

            if (storageComp.Whitelist != null && !storageComp.Whitelist.IsValid(insertEnt))
                return false;

            if (TryComp<TransformComponent>(insertEnt, out var transformComp) && transformComp.Anchored)
                return false;

            return true;
        }

        /// <summary>
        ///     Inserts into the storage container
        /// </summary>
        /// <param name="entity">The entity to insert</param>
        /// <returns>true if the entity was inserted, false otherwise</returns>
        public bool Insert(EntityUid uid, EntityUid insertEnt, ServerStorageComponent storageComp)
        {
            if (!CanInsert(uid, insertEnt, storageComp) || storageComp.Storage?.Insert(insertEnt) == false)
                return false;

            UpdateStorageUI(uid, storageComp);
            return true;
        }

        public bool Remove(EntityUid uid, EntityUid removeEnt, ServerStorageComponent storageComp)
        {
            EnsureInitialCalculated(uid, storageComp);
            return storageComp.Storage?.Remove(removeEnt) == true;
        }

        public void HandleEntityMaybeInserted(EntityUid uid, EntInsertedIntoContainerMessage message, ServerStorageComponent storageComp)
        {
            // as if it would a SEPERATE container? what?
            // if (message.Container != storageComp.Storage)
            //     return;

            PlaySoundCollection(uid, storageComp);
            EnsureInitialCalculated(uid, storageComp);

            Logger.DebugS(storageComp.LoggerName, $"Storage (UID {uid}) had entity (UID {message.Entity}) inserted into it.");

            var size = 0;
            if (TryComp<SharedItemComponent>(message.Entity, out var storable))
                size = storable.Size;

            storageComp.StorageUsed += size;
            storageComp.SizeCache[message.Entity] = size;

        }

        public void HandleEntityMaybeRemoved(EntityUid uid, EntRemovedFromContainerMessage message, ServerStorageComponent storageComp )
        {

            // what do you mean MAYBE
            // seems to just make sure it can still fit

            if (message.Container != storageComp.Storage)
                return;

            EnsureInitialCalculated(uid, storageComp);

            Logger.DebugS(storageComp.LoggerName, $"Storage (UID {uid}) had entity (UID {message.Entity}) removed from it.");

            if (!storageComp.SizeCache.TryGetValue(message.Entity, out var size))
            {
                var mapPos = "unknown";
                if (TryComp<TransformComponent>(uid, out var transformComp))
                    mapPos = transformComp.MapPosition.ToString();

                Logger.WarningS(storageComp.LoggerName, $"Removed entity {ToPrettyString(message.Entity)} without a cached size from storage {ToPrettyString(uid)} at {mapPos}");

                RecalculateStorageUsed(uid, storageComp);
                return;
            }

            storageComp.StorageUsed -= size;

            // UpdateClientInventories(uid, storageComp);
        }

        /// <summary>
        ///     Inserts an entity into storage from the player's active hand
        /// </summary>
        /// <param name="player">The player to insert an entity from</param>
        /// <returns>true if inserted, false otherwise</returns>
        public bool PlayerInsertHeldEntity(EntityUid uid, EntityUid player, ServerStorageComponent storageComp)
        {
            //  they're the same THING!

            EnsureInitialCalculated(uid, storageComp);

            if (!TryComp<HandsComponent>(player, out var hands) ||
                hands.ActiveHandEntity == null)
                return false;

            var toInsert = hands.ActiveHandEntity;

            if (!_sharedHandsSystem.TryDrop(player, toInsert.Value, handsComp: hands))
            {
                uid.PopupMessage(player, Loc.GetString("comp-storage-cant-insert"));
                return false;
            }

            if (!Insert(uid, toInsert.Value, storageComp))
            {
                _sharedHandsSystem.PickupOrDrop(player, toInsert.Value, handsComp: hands);
                uid.PopupMessage(player, Loc.GetString("comp-storage-cant-insert"));
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Inserts an Entity (<paramref name="toInsert"/>) in the world into storage, informing <paramref name="player"/> if it fails.
        ///     <paramref name="toInsert"/> is *NOT* held, see <see cref="PlayerInsertHeldEntity(Robust.Shared.GameObjects.EntityUid)"/>.
        /// </summary>
        /// <param name="player">The player to insert an entity with</param>
        /// <returns>true if inserted, false otherwise</returns>
        public bool PlayerInsertEntityInWorld(EntityUid uid, EntityUid player, EntityUid toInsert, ServerStorageComponent storageComp)
        {

            // just instert into container?
            EnsureInitialCalculated(uid, storageComp);

            if (!Insert(uid, toInsert, storageComp))
            {
                // NEED NEW POPUPS HERE
                uid.PopupMessage(player, Loc.GetString("comp-storage-cant-insert"));
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Opens the storage UI for an entity
        /// </summary>
        /// <param name="entity">The entity to open the UI for</param>
        public void OpenStorageUI(EntityUid uid, EntityUid entity, ServerStorageComponent storageComp)
        {
            // just open bound user interfaaaace
            PlaySoundCollection(uid, storageComp);
            EnsureInitialCalculated(uid, storageComp);

            if (!TryComp<ActorComponent>(entity, out var player))
                return;

            Logger.DebugS(storageComp.LoggerName, $"Storage (UID {uid}) \"used\" by player session (UID {player.PlayerSession.AttachedEntity}).");

            _uiSystem.GetUiOrNull(uid, StorageUiKey.Key)?.Open(player.PlayerSession);
            // UpdateClientInventory(uid, player.PlayerSession, storageComp);
        }

        /// <summary>
        ///     If the user has nested-UIs open (e.g., PDA UI open when pda is in a backpack), close them.
        /// </summary>
        /// <param name="session"></param>
        public void CloseNestedInterfaces(EntityUid uid, IPlayerSession session, ServerStorageComponent storageComp)
        {
            if (storageComp.StoredEntities == null)
                return;

            // for each containing thing
            // if it has a storage comp
            // ensure unsubscribe from session
            // if it has a ui component
            // close ui
            foreach (var entity in storageComp.StoredEntities)
            {
                if (TryComp<ServerStorageComponent>(entity, out var storedStorageComp))
                {
                    DebugTools.Assert(storedStorageComp != storageComp, $"Storage component contains itself!? Entity: {uid}");
                    // UnsubscribeSession(entity, session, storedStorageComp);
                }

                if (TryComp<ServerUserInterfaceComponent>(entity, out var uiComponent))
                {
                    foreach (var ui in uiComponent.Interfaces)
                    {
                        ui.Close(session);
                    }
                }
            }
        }

        private void OnComponentInit(EntityUid uid, ServerStorageComponent storageComp, ComponentInit args)
        {
            base.Initialize();

            // ReSharper disable once StringLiteralTypo
            storageComp.Storage = _containerSystem.EnsureContainer<Container>(uid, "storagebase");
            storageComp.Storage.OccludesLight = storageComp.OccludesLight;
            UpdateStorageVisualization(uid, storageComp);
            EnsureInitialCalculated(uid, storageComp);
            UpdateStorageUI(uid, storageComp);
        }

        public void HandleRemoveEntity(EntityUid uid, EntityUid player, EntityUid itemToRemove, ServerStorageComponent storageComp)
        {
            // why should we care about size, we're removing
            // EnsureInitialCalculated(uid, storageComp);

            // need to make sure player stuff is correct

            // ENSURE THAT It'S IN THE FUCKING CONTAINER

            // probably pop that not found
            if (!_containerSystem.ContainsEntity(uid, itemToRemove))
                return;


            // succeeded, remove entity and update UI
            _containerSystem.RemoveEntity(uid, itemToRemove, false);
            _sharedHandsSystem.TryPickupAnyHand(player, itemToRemove);
            UpdateStorageUI(uid, storageComp);
        }

        /// <summary>
        /// Inserts storable entities into this storage container if possible, otherwise return to the hand of the user
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns>true if inserted, false otherwise</returns>
        private void OnInteractUsing(EntityUid uid, ServerStorageComponent storageComp, InteractUsingEvent args)
        {
            if (!storageComp.ClickInsert)
                return;

            Logger.DebugS(storageComp.LoggerName, $"Storage (UID {uid}) attacked by user (UID {args.User}) with entity (UID {args.Used}).");

            if (HasComp<PlaceableSurfaceComponent>(uid))
                return;

            PlayerInsertHeldEntity(uid, args.User, storageComp);
        }

        /// <summary>
        /// Sends a message to open the storage UI
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        private void OnActivate(EntityUid uid, ServerStorageComponent storageComp, ActivateInWorldEvent args)
        {
            Logger.Debug("Activate");

            EnsureInitialCalculated(uid, storageComp);

            if (!TryComp<ActorComponent>(args.User, out var actor))
                return;

            Logger.Debug("Open storage uI");
            OpenStorageUI(uid, args.User, storageComp);
        }

        /// <summary>
        /// Allows a user to pick up entities by clicking them, or pick up all entities in a certain radius
        /// arround a click.
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        private void AfterInteract(EntityUid uid, ServerStorageComponent storageComp, AfterInteractEvent eventArgs)
        {
            if (!eventArgs.CanReach) return;

            // Pick up all entities in a radius around the clicked location.
            // The last half of the if is because carpets exist and this is terrible
            if (storageComp.AreaInsert && (eventArgs.Target == null || !HasComp<SharedItemComponent>(eventArgs.Target.Value)))
            {
                var validStorables = new List<EntityUid>();
                foreach (var entity in _entityLookupSystem.GetEntitiesInRange(eventArgs.ClickLocation, storageComp.AreaInsertRadius, LookupFlags.None))
                {
                    if (_containerSystem.IsEntityInContainer(entity)
                        || entity == eventArgs.User
                        || !HasComp<SharedItemComponent>(entity)
                        || !_interactionSystem.InRangeUnobstructed(eventArgs.User, entity))
                        continue;
                    validStorables.Add(entity);
                }

                //If there's only one then let's be generous
                if (validStorables.Count > 1)
                {
                    var doAfterArgs = new DoAfterEventArgs(eventArgs.User, 0.2f * validStorables.Count, CancellationToken.None, uid)
                    {
                        BreakOnStun = true,
                        BreakOnDamage = true,
                        BreakOnUserMove = true,
                        NeedHand = true,
                    };
                    var result = _doAfterSystem.WaitDoAfter(doAfterArgs);
                    // if (result != DoAfterStatus.Finished) return true;
                }

                var successfullyInserted = new List<EntityUid>();
                var successfullyInsertedPositions = new List<EntityCoordinates>();
                foreach (var entity in validStorables)
                {
                    // Check again, situation may have changed for some entities, but we'll still pick up any that are valid
                    if (_containerSystem.IsEntityInContainer(entity)
                        || entity == eventArgs.User
                        || !HasComp<SharedItemComponent>(entity))
                        continue;

                    if (TryComp<TransformComponent>(uid, out var transformOwner) && TryComp<TransformComponent>(entity, out var transformEnt))
                    {
                        var position = EntityCoordinates.FromMap(transformOwner.Parent?.Owner ?? uid, transformEnt.MapPosition);

                        if (PlayerInsertEntityInWorld(uid, eventArgs.User, entity, storageComp))
                        {
                            successfullyInserted.Add(entity);
                            successfullyInsertedPositions.Add(position);
                        }
                    }
                }

                // If we picked up atleast one thing, play a sound and do a cool animation!
                if (successfullyInserted.Count > 0)
                {
                    PlaySoundCollection(uid, storageComp);
                    RaiseLocalEvent(new AnimateInsertingEntitiesEvent(uid, successfullyInserted, successfullyInsertedPositions));
                }
                return;
            }
            // Pick up the clicked entity
            else if (storageComp.QuickInsert)
            {
                if (eventArgs.Target is not {Valid: true} target)
                    return;

                if (_containerSystem.IsEntityInContainer(target)
                    || target == eventArgs.User
                    || !HasComp<SharedItemComponent>(target))
                    return;

                if (TryComp<TransformComponent>(uid, out var transformOwner) && TryComp<TransformComponent>(target, out var transformEnt))
                {
                    var position = EntityCoordinates.FromMap(
                    transformOwner.Parent?.Owner ?? uid,
                    transformEnt.MapPosition);
                    if (PlayerInsertEntityInWorld(uid, eventArgs.User, target, storageComp))
                    {
                        RaiseLocalEvent(new AnimateInsertingEntitiesEvent(uid,
                            new List<EntityUid> { target },
                            new List<EntityCoordinates> { position }));
                        return;
                    }
                }
                return;
            }
            return;
        }

        private void OnDestroy(EntityUid uid, ServerStorageComponent storageComp, DestructionEventArgs args)
        {
            var storedEntities = storageComp.StoredEntities?.ToList();

            if (storedEntities == null)
                return;

            foreach (var entity in storedEntities)
            {
                Remove(uid, entity, storageComp);
            }
        }

        private void OnUIInteractMessage(EntityUid uid, ServerStorageComponent storageComp, StorageInteractItemMessage args)
        {
            Logger.Debug("attempted on item remove");
            if (args.Session.AttachedEntity == null)
                return;

            HandleRemoveEntity(uid, args.Session.AttachedEntity.Value, args.InteractedItemUID, storageComp);

        }

        private void PlaySoundCollection(EntityUid uid, ServerStorageComponent storageComp)
        {
            SoundSystem.Play(Filter.Pvs(uid), storageComp.StorageSoundCollection.GetSound(), uid, AudioParams.Default);
        }

        private void UpdateStorageUI(EntityUid uid, ServerStorageComponent storageComp)
        {

            if (storageComp.Storage == null)
                return;

            var state = new StorageBoundUserInterfaceState(storageComp.Storage.ContainedEntities, storageComp.StorageUsed, storageComp.StorageCapacityMax);

            _uiSystem.GetUiOrNull(uid, StorageUiKey.Key)?.SetState(state);
        }

    }
}
