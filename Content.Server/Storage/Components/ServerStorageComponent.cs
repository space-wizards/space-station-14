using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.DoAfter;
using Content.Server.Hands.Components;
using Content.Server.Interaction;
using Content.Shared.Acts;
using Content.Shared.Coordinates;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Item;
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Players;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.Storage.Components
{
    /// <summary>
    /// Storage component for containing entities within this one, matches a UI on the client which shows stored entities
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IStorageComponent))]
    [ComponentReference(typeof(SharedStorageComponent))]
    public sealed class ServerStorageComponent : SharedStorageComponent, IInteractUsing, IActivate, IStorageComponent, IDestroyAct, IAfterInteract
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private const string LoggerName = "Storage";

        public Container? Storage;

        private readonly Dictionary<EntityUid, int> _sizeCache = new();

        [DataField("occludesLight")]
        private bool _occludesLight = true;

        [DataField("quickInsert")]
        private bool _quickInsert = false; // Can insert storables by "attacking" them with the storage entity

        [DataField("clickInsert")]
        private bool _clickInsert = true; // Can insert stuff by clicking the storage entity with it

        [DataField("areaInsert")]
        private bool _areaInsert = false;  // "Attacking" with the storage entity causes it to insert all nearby storables after a delay
        [DataField("areaInsertRadius")]
        private int _areaInsertRadius = 1;

        [DataField("whitelist")]
        private EntityWhitelist? _whitelist = null;

        private bool _storageInitialCalculated;
        private int _storageUsed;
        [DataField("capacity")]
        private int _storageCapacityMax = 10000;
        public readonly HashSet<IPlayerSession> SubscribedSessions = new();

        [DataField("storageSoundCollection")]
        public SoundSpecifier StorageSoundCollection { get; set; } = new SoundCollectionSpecifier("storageRustle");

        [ViewVariables]
        public override IReadOnlyList<EntityUid>? StoredEntities => Storage?.ContainedEntities;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool OccludesLight
        {
            get => _occludesLight;
            set
            {
                _occludesLight = value;
                if (Storage != null) Storage.OccludesLight = value;
            }
        }

        private void UpdateStorageVisualization()
        {
            if (!_entityManager.TryGetComponent(Owner, out AppearanceComponent appearance))
                return;

            bool open = SubscribedSessions.Count != 0;

            appearance.SetData(StorageVisuals.Open, open);
            appearance.SetData(SharedBagOpenVisuals.BagState, open ? SharedBagState.Open : SharedBagState.Closed);

            if (_entityManager.HasComponent<ItemCounterComponent>(Owner))
                appearance.SetData(StackVisuals.Hide, !open);
        }

        private void EnsureInitialCalculated()
        {
            if (_storageInitialCalculated)
            {
                return;
            }

            RecalculateStorageUsed();

            _storageInitialCalculated = true;
        }

        private void RecalculateStorageUsed()
        {
            _storageUsed = 0;
            _sizeCache.Clear();

            if (Storage == null)
            {
                return;
            }

            foreach (var entity in Storage.ContainedEntities)
            {
                var item = _entityManager.GetComponent<SharedItemComponent>(entity);
                _storageUsed += item.Size;
                _sizeCache.Add(entity, item.Size);
            }
        }

        /// <summary>
        ///     Verifies if an entity can be stored and if it fits
        /// </summary>
        /// <param name="entity">The entity to check</param>
        /// <returns>true if it can be inserted, false otherwise</returns>
        public bool CanInsert(EntityUid entity)
        {
            EnsureInitialCalculated();

            if (_entityManager.TryGetComponent(entity, out ServerStorageComponent? storage) &&
                storage._storageCapacityMax >= _storageCapacityMax)
            {
                return false;
            }

            if (_entityManager.TryGetComponent(entity, out SharedItemComponent? store) &&
                store.Size > _storageCapacityMax - _storageUsed)
            {
                return false;
            }

            if (_whitelist != null && !_whitelist.IsValid(entity))
            {
                return false;
            }

            if (_entityManager.GetComponent<TransformComponent>(entity).Anchored)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Inserts into the storage container
        /// </summary>
        /// <param name="entity">The entity to insert</param>
        /// <returns>true if the entity was inserted, false otherwise</returns>
        public bool Insert(EntityUid entity)
        {
            return CanInsert(entity) && Storage?.Insert(entity) == true;
        }

        public override bool Remove(EntityUid entity)
        {
            EnsureInitialCalculated();
            return Storage?.Remove(entity) == true;
        }

        public void HandleEntityMaybeInserted(EntInsertedIntoContainerMessage message)
        {
            if (message.Container != Storage)
            {
                return;
            }

            PlaySoundCollection();
            EnsureInitialCalculated();

            Logger.DebugS(LoggerName, $"Storage (UID {Owner}) had entity (UID {message.Entity}) inserted into it.");

            var size = 0;
            if (_entityManager.TryGetComponent(message.Entity, out SharedItemComponent? storable))
                size = storable.Size;

            _storageUsed += size;
            _sizeCache[message.Entity] = size;

            UpdateClientInventories();
        }

        public void HandleEntityMaybeRemoved(EntRemovedFromContainerMessage message)
        {
            if (message.Container != Storage)
            {
                return;
            }

            EnsureInitialCalculated();

            Logger.DebugS(LoggerName, $"Storage (UID {Owner}) had entity (UID {message.Entity}) removed from it.");

            if (!_sizeCache.TryGetValue(message.Entity, out var size))
            {
                Logger.WarningS(LoggerName, $"Removed entity {_entityManager.ToPrettyString(message.Entity)} without a cached size from storage {_entityManager.ToPrettyString(Owner)} at {_entityManager.GetComponent<TransformComponent>(Owner).MapPosition}");

                RecalculateStorageUsed();
                return;
            }

            _storageUsed -= size;

            UpdateClientInventories();
        }

        /// <summary>
        ///     Inserts an entity into storage from the player's active hand
        /// </summary>
        /// <param name="player">The player to insert an entity from</param>
        /// <returns>true if inserted, false otherwise</returns>
        public bool PlayerInsertHeldEntity(EntityUid player)
        {
            EnsureInitialCalculated();

            if (!_entityManager.TryGetComponent(player, out HandsComponent? hands) ||
                hands.GetActiveHandItem == null)
            {
                return false;
            }

            var toInsert = hands.GetActiveHandItem;

            if (!hands.Drop(toInsert.Owner))
            {
                Owner.PopupMessage(player, "Can't insert.");
                return false;
            }

            if (!Insert(toInsert.Owner))
            {
                hands.PutInHand(toInsert);
                Owner.PopupMessage(player, "Can't insert.");
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
        public bool PlayerInsertEntityInWorld(EntityUid player, EntityUid toInsert)
        {
            EnsureInitialCalculated();

            if (!Insert(toInsert))
            {
                Owner.PopupMessage(player, "Can't insert.");
                return false;
            }
            return true;
        }

        /// <summary>
        ///     Opens the storage UI for an entity
        /// </summary>
        /// <param name="entity">The entity to open the UI for</param>
        public void OpenStorageUI(EntityUid entity)
        {
            PlaySoundCollection();
            EnsureInitialCalculated();

            var userSession = _entityManager.GetComponent<ActorComponent>(entity).PlayerSession;

            Logger.DebugS(LoggerName, $"Storage (UID {Owner}) \"used\" by player session (UID {userSession.AttachedEntity}).");

            SubscribeSession(userSession);
#pragma warning disable 618
            SendNetworkMessage(new OpenStorageUIMessage(), userSession.ConnectedClient);
#pragma warning restore 618
            UpdateClientInventory(userSession);
        }

        /// <summary>
        ///     Updates the storage UI on all subscribed actors, informing them of the state of the container.
        /// </summary>
        private void UpdateClientInventories()
        {
            foreach (var session in SubscribedSessions)
            {
                UpdateClientInventory(session);
            }
        }

        /// <summary>
        ///     Updates storage UI on a client, informing them of the state of the container.
        /// </summary>
        /// <param name="session">The client to be updated</param>
        private void UpdateClientInventory(IPlayerSession session)
        {
            if (session.AttachedEntity == null)
            {
                Logger.DebugS(LoggerName, $"Storage (UID {Owner}) detected no attached entity in player session (UID {session.AttachedEntity}).");

                UnsubscribeSession(session);
                return;
            }

            if (Storage == null)
            {
                Logger.WarningS(LoggerName, $"{nameof(UpdateClientInventory)} called with null {nameof(Storage)}");

                return;
            }

            if (StoredEntities == null)
            {
                Logger.WarningS(LoggerName, $"{nameof(UpdateClientInventory)} called with null {nameof(StoredEntities)}");

                return;
            }

            var stored = StoredEntities.Select(e => e).ToArray();

#pragma warning disable 618
            SendNetworkMessage(new StorageHeldItemsMessage(stored, _storageUsed, _storageCapacityMax), session.ConnectedClient);
#pragma warning restore 618
        }

        /// <summary>
        ///     Adds a session to the update list.
        /// </summary>
        /// <param name="session">The session to add</param>
        private void SubscribeSession(IPlayerSession session)
        {
            EnsureInitialCalculated();

            if (!SubscribedSessions.Contains(session))
            {
                Logger.DebugS(LoggerName, $"Storage (UID {Owner}) subscribed player session (UID {session.AttachedEntity}).");

                session.PlayerStatusChanged += HandlePlayerSessionChangeEvent;
                SubscribedSessions.Add(session);
            }

            if (SubscribedSessions.Count == 1)
                UpdateStorageVisualization();
        }

        /// <summary>
        ///     Removes a session from the update list.
        /// </summary>
        /// <param name="session">The session to remove</param>
        public void UnsubscribeSession(IPlayerSession session)
        {
            if (SubscribedSessions.Contains(session))
            {
                Logger.DebugS(LoggerName, $"Storage (UID {Owner}) unsubscribed player session (UID {session.AttachedEntity}).");

                SubscribedSessions.Remove(session);
#pragma warning disable 618
                SendNetworkMessage(new CloseStorageUIMessage(), session.ConnectedClient);
#pragma warning restore 618
            }

            CloseNestedInterfaces(session);

            if (SubscribedSessions.Count == 0)
                UpdateStorageVisualization();
        }

        /// <summary>
        ///     If the user has nested-UIs open (e.g., PDA UI open when pda is in a backpack), close them.
        /// </summary>
        /// <param name="session"></param>
        public void CloseNestedInterfaces(IPlayerSession session)
        {
            if (StoredEntities == null)
                return;

            foreach (var entity in StoredEntities)
            {
                if (_entityManager.TryGetComponent(entity, out ServerStorageComponent storageComponent))
                {
                    DebugTools.Assert(storageComponent != this, $"Storage component contains itself!? Entity: {Owner}");
                    storageComponent.UnsubscribeSession(session);
                }

                if (_entityManager.TryGetComponent(entity, out ServerUserInterfaceComponent uiComponent))
                {
                    foreach (var ui in uiComponent.Interfaces)
                    {
                        ui.Close(session);
                    }
                }
            }
        }

        private void HandlePlayerSessionChangeEvent(object? obj, SessionStatusEventArgs sessionStatus)
        {
            Logger.DebugS(LoggerName, $"Storage (UID {Owner}) handled a status change in player session (UID {sessionStatus.Session.AttachedEntity}).");

            if (sessionStatus.NewStatus != SessionStatus.InGame)
            {
                UnsubscribeSession(sessionStatus.Session);
            }
        }

        protected override void Initialize()
        {
            base.Initialize();

            // ReSharper disable once StringLiteralTypo
            Storage = Owner.EnsureContainer<Container>("storagebase");
            Storage.OccludesLight = _occludesLight;
            UpdateStorageVisualization();
            EnsureInitialCalculated();
        }

        [Obsolete("Component Messages are deprecated, use Entity Events instead.")]
        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession? session = null)
        {
            base.HandleNetworkMessage(message, channel, session);

            if (session == null)
            {
                throw new ArgumentException(nameof(session));
            }

            switch (message)
            {
                case RemoveEntityMessage remove:
                {
                    EnsureInitialCalculated();

                    if (session.AttachedEntity is not {Valid: true} player)
                    {
                        break;
                    }

                    var ownerTransform = _entityManager.GetComponent<TransformComponent>(Owner);
                    var playerTransform = _entityManager.GetComponent<TransformComponent>(player);

                    if (!playerTransform.Coordinates.InRange(_entityManager, ownerTransform.Coordinates, 2) ||
                        Owner.IsInContainer() && !playerTransform.ContainsEntity(ownerTransform))
                    {
                        break;
                    }

                    if (!remove.EntityUid.Valid || Storage?.Contains(remove.EntityUid) == false)
                    {
                        break;
                    }

                    if (!_entityManager.TryGetComponent(remove.EntityUid, out SharedItemComponent? item) || !_entityManager.TryGetComponent(player, out HandsComponent? hands))
                    {
                        break;
                    }

                    if (!hands.CanPutInHand(item))
                    {
                        break;
                    }

                    hands.PutInHand(item);

                    break;
                }
                case InsertEntityMessage _:
                {
                    EnsureInitialCalculated();

                    if (session.AttachedEntity is not {Valid: true} player)
                    {
                        break;
                    }

                    if (!EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(player, Owner, popup: true))
                    {
                        break;
                    }

                    PlayerInsertHeldEntity(player);

                    break;
                }
                case CloseStorageUIMessage _:
                {
                    if (session is not IPlayerSession playerSession)
                    {
                        break;
                    }

                    UnsubscribeSession(playerSession);
                    break;
                }
            }
        }

        /// <summary>
        /// Inserts storable entities into this storage container if possible, otherwise return to the hand of the user
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns>true if inserted, false otherwise</returns>
        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!_clickInsert)
                return false;
            Logger.DebugS(LoggerName, $"Storage (UID {Owner}) attacked by user (UID {eventArgs.User}) with entity (UID {eventArgs.Using}).");

            if (_entityManager.HasComponent<PlaceableSurfaceComponent>(Owner))
            {
                return false;
            }

            return PlayerInsertHeldEntity(eventArgs.User);
        }

        /// <summary>
        /// Sends a message to open the storage UI
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            EnsureInitialCalculated();
            OpenStorageUI(eventArgs.User);
        }

        /// <summary>
        /// Allows a user to pick up entities by clicking them, or pick up all entities in a certain radius
        /// arround a click.
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (!eventArgs.CanReach) return false;

            // Pick up all entities in a radius around the clicked location.
            // The last half of the if is because carpets exist and this is terrible
            if (_areaInsert && (eventArgs.Target == null || !_entityManager.HasComponent<SharedItemComponent>(eventArgs.Target.Value)))
            {
                var validStorables = new List<EntityUid>();
                foreach (var entity in IoCManager.Resolve<IEntityLookup>().GetEntitiesInRange(eventArgs.ClickLocation, _areaInsertRadius, LookupFlags.None))
                {
                    if (entity.IsInContainer()
                        || entity == eventArgs.User
                        || !_entityManager.HasComponent<SharedItemComponent>(entity)
                        || !EntitySystem.Get<InteractionSystem>().InRangeUnobstructed(eventArgs.User, entity))
                        continue;
                    validStorables.Add(entity);
                }

                //If there's only one then let's be generous
                if (validStorables.Count > 1)
                {
                    var doAfterSystem = EntitySystem.Get<DoAfterSystem>();
                    var doAfterArgs = new DoAfterEventArgs(eventArgs.User, 0.2f * validStorables.Count, CancellationToken.None, Owner)
                    {
                        BreakOnStun = true,
                        BreakOnDamage = true,
                        BreakOnUserMove = true,
                        NeedHand = true,
                    };
                    var result = await doAfterSystem.WaitDoAfter(doAfterArgs);
                    if (result != DoAfterStatus.Finished) return true;
                }

                var successfullyInserted = new List<EntityUid>();
                var successfullyInsertedPositions = new List<EntityCoordinates>();
                foreach (var entity in validStorables)
                {
                    // Check again, situation may have changed for some entities, but we'll still pick up any that are valid
                    if (entity.IsInContainer()
                        || entity == eventArgs.User
                        || !_entityManager.HasComponent<SharedItemComponent>(entity))
                        continue;
                    var position = EntityCoordinates.FromMap(_entityManager.GetComponent<TransformComponent>(Owner).Parent?.Owner ?? Owner, _entityManager.GetComponent<TransformComponent>(entity).MapPosition);
                    if (PlayerInsertEntityInWorld(eventArgs.User, entity))
                    {
                        successfullyInserted.Add(entity);
                        successfullyInsertedPositions.Add(position);
                    }
                }

                // If we picked up atleast one thing, play a sound and do a cool animation!
                if (successfullyInserted.Count > 0)
                {
                    PlaySoundCollection();
#pragma warning disable 618
                    SendNetworkMessage(
#pragma warning restore 618
                        new AnimateInsertingEntitiesMessage(
                            successfullyInserted,
                            successfullyInsertedPositions
                        )
                    );
                }
                return true;
            }
            // Pick up the clicked entity
            else if (_quickInsert)
            {
                if (eventArgs.Target is not {Valid: true} target)
                {
                    return false;
                }

                if (target.IsInContainer()
                    || target == eventArgs.User
                    || !_entityManager.HasComponent<SharedItemComponent>(target))
                    return false;
                var position = EntityCoordinates.FromMap(
                    _entityManager.GetComponent<TransformComponent>(Owner).Parent?.Owner ?? Owner,
                    _entityManager.GetComponent<TransformComponent>(target).MapPosition);
                if (PlayerInsertEntityInWorld(eventArgs.User, target))
                {
#pragma warning disable 618
                    SendNetworkMessage(new AnimateInsertingEntitiesMessage(
#pragma warning restore 618
                        new List<EntityUid> {target},
                        new List<EntityCoordinates> {position}
                    ));
                    return true;
                }
                return true;
            }
            return false;
        }

        void IDestroyAct.OnDestroy(DestructionEventArgs eventArgs)
        {
            var storedEntities = StoredEntities?.ToList();

            if (storedEntities == null)
            {
                return;
            }

            foreach (var entity in storedEntities)
            {
                Remove(entity);
            }
        }

        private void PlaySoundCollection()
        {
            SoundSystem.Play(Filter.Pvs(Owner), StorageSoundCollection.GetSound(), Owner, AudioParams.Default);
        }
    }
}
