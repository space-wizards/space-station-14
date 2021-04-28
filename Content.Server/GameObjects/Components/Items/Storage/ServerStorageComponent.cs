#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Shared.Audio;
using Content.Shared.GameObjects.Components.Storage;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
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
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Items.Storage
{
    /// <summary>
    /// Storage component for containing entities within this one, matches a UI on the client which shows stored entities
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IStorageComponent))]
    public class ServerStorageComponent : SharedStorageComponent, IInteractUsing, IUse, IActivate, IStorageComponent, IDestroyAct, IExAct, IAfterInteract
    {
        private const string LoggerName = "Storage";

        private Container? _storage;
        private readonly Dictionary<IEntity, int> _sizeCache = new();

        [DataField("occludesLight")]
        private bool _occludesLight = true;
        [DataField("quickInsert")]
        private bool _quickInsert; //Can insert storables by "attacking" them with the storage entity
        [DataField("areaInsert")]
        private bool _areaInsert;  //"Attacking" with the storage entity causes it to insert all nearby storables after a delay
        private bool _storageInitialCalculated;
        private int _storageUsed;
        [DataField("capacity")]
        private int _storageCapacityMax = 10000;
        [DataField("showFillLevel")]
        private bool _showFillLevel = false;
        public readonly HashSet<IPlayerSession> SubscribedSessions = new();

        [DataField("storageSoundCollection")]
        public string? StorageSoundCollection { get; set; }

        [ComponentDependency] private readonly AppearanceComponent? _appearanceComponent = default;

        [ViewVariables]
        public override IReadOnlyList<IEntity>? StoredEntities => _storage?.ContainedEntities;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool OccludesLight
        {
            get => _occludesLight;
            set
            {
                _occludesLight = value;
                if (_storage != null) _storage.OccludesLight = value;
            }
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

            if (_storage == null)
            {
                return;
            }

            foreach (var entity in _storage.ContainedEntities)
            {
                var item = entity.GetComponent<SharedItemComponent>();
                _storageUsed += item.Size;
            }
        }

        /// <summary>
        ///     Verifies if an entity can be stored and if it fits
        /// </summary>
        /// <param name="entity">The entity to check</param>
        /// <returns>true if it can be inserted, false otherwise</returns>
        public bool CanInsert(IEntity entity)
        {
            EnsureInitialCalculated();

            if (entity.TryGetComponent(out ServerStorageComponent? storage) &&
                storage._storageCapacityMax >= _storageCapacityMax)
            {
                return false;
            }

            if (entity.TryGetComponent(out SharedItemComponent? store) &&
                store.Size > _storageCapacityMax - _storageUsed)
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
        public bool Insert(IEntity entity)
        {
            return CanInsert(entity) && _storage?.Insert(entity) == true;
        }

        public override bool Remove(IEntity entity)
        {
            EnsureInitialCalculated();
            return _storage?.Remove(entity) == true;
        }

        public void HandleEntityMaybeInserted(EntInsertedIntoContainerMessage message)
        {
            if (message.Container != _storage)
            {
                return;
            }

            PlayStorageSound();
            EnsureInitialCalculated();

            Logger.DebugS(LoggerName, $"Storage (UID {Owner.Uid}) had entity (UID {message.Entity.Uid}) inserted into it.");

            var size = 0;
            if (message.Entity.TryGetComponent(out SharedItemComponent? storable))
                size = storable.Size;

            _storageUsed += size;
            _sizeCache[message.Entity] = size;

            UpdateFillLevelVisualizer();
            UpdateClientInventories();
        }

        public void HandleEntityMaybeRemoved(EntRemovedFromContainerMessage message)
        {
            if (message.Container != _storage)
            {
                return;
            }

            EnsureInitialCalculated();

            Logger.DebugS(LoggerName, $"Storage (UID {Owner}) had entity (UID {message.Entity}) removed from it.");

            if (!_sizeCache.TryGetValue(message.Entity, out var size))
            {
                Logger.WarningS(LoggerName, $"Removed entity {message.Entity} without a cached size from storage {Owner} at {Owner.Transform.MapPosition}");

                RecalculateStorageUsed();
                return;
            }

            _storageUsed -= size;

            UpdateFillLevelVisualizer();
            UpdateClientInventories();
        }

        private void UpdateFillLevelVisualizer()
        {
            // update visualizer if needed
            if (_showFillLevel && _appearanceComponent != null)
            {
                var state = new StorageFillLevel(_storageUsed, _storageCapacityMax);
                _appearanceComponent.SetData(StorageVisuals.FillLevel, state);
            }
        }

        /// <summary>
        ///     Inserts an entity into storage from the player's active hand
        /// </summary>
        /// <param name="player">The player to insert an entity from</param>
        /// <returns>true if inserted, false otherwise</returns>
        public bool PlayerInsertHeldEntity(IEntity player)
        {
            EnsureInitialCalculated();

            if (!player.TryGetComponent(out IHandsComponent? hands) ||
                hands.GetActiveHand == null)
            {
                return false;
            }

            var toInsert = hands.GetActiveHand;

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
        ///     <paramref name="toInsert"/> is *NOT* held, see <see cref="PlayerInsertHeldEntity(IEntity)"/>.
        /// </summary>
        /// <param name="player">The player to insert an entity with</param>
        /// <returns>true if inserted, false otherwise</returns>
        public bool PlayerInsertEntityInWorld(IEntity player, IEntity toInsert)
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
        public void OpenStorageUI(IEntity entity)
        {
            PlayStorageSound();
            EnsureInitialCalculated();

            var userSession = entity.GetComponent<BasicActorComponent>().playerSession;

            Logger.DebugS(LoggerName, $"Storage (UID {Owner.Uid}) \"used\" by player session (UID {userSession.AttachedEntityUid}).");

            SubscribeSession(userSession);
            SendNetworkMessage(new OpenStorageUIMessage(), userSession.ConnectedClient);
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
                Logger.DebugS(LoggerName, $"Storage (UID {Owner.Uid}) detected no attached entity in player session (UID {session.AttachedEntityUid}).");

                UnsubscribeSession(session);
                return;
            }

            if (_storage == null)
            {
                Logger.WarningS(LoggerName, $"{nameof(UpdateClientInventory)} called with null {nameof(_storage)}");

                return;
            }

            if (StoredEntities == null)
            {
                Logger.WarningS(LoggerName, $"{nameof(UpdateClientInventory)} called with null {nameof(StoredEntities)}");

                return;
            }

            var stored = StoredEntities.Select(e => e.Uid).ToArray();

            SendNetworkMessage(new StorageHeldItemsMessage(stored, _storageUsed, _storageCapacityMax), session.ConnectedClient);
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
                Logger.DebugS(LoggerName, $"Storage (UID {Owner.Uid}) subscribed player session (UID {session.AttachedEntityUid}).");

                session.PlayerStatusChanged += HandlePlayerSessionChangeEvent;
                SubscribedSessions.Add(session);

                UpdateDoorState();
            }
        }

        /// <summary>
        ///     Removes a session from the update list.
        /// </summary>
        /// <param name="session">The session to remove</param>
        public void UnsubscribeSession(IPlayerSession session)
        {
            if (SubscribedSessions.Contains(session))
            {
                Logger.DebugS(LoggerName, $"Storage (UID {Owner.Uid}) unsubscribed player session (UID {session.AttachedEntityUid}).");

                SubscribedSessions.Remove(session);
                SendNetworkMessage(new CloseStorageUIMessage(), session.ConnectedClient);

                UpdateDoorState();
            }
        }

        private void HandlePlayerSessionChangeEvent(object? obj, SessionStatusEventArgs sessionStatus)
        {
            Logger.DebugS(LoggerName, $"Storage (UID {Owner.Uid}) handled a status change in player session (UID {sessionStatus.Session.AttachedEntityUid}).");

            if (sessionStatus.NewStatus != SessionStatus.InGame)
            {
                UnsubscribeSession(sessionStatus.Session);
            }
        }

        private void UpdateDoorState()
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(StorageVisuals.Open, SubscribedSessions.Count != 0);
            }
        }

        public override void Initialize()
        {
            base.Initialize();

            // ReSharper disable once StringLiteralTypo
            _storage = ContainerHelpers.EnsureContainer<Container>(Owner, "storagebase");
            _storage.OccludesLight = _occludesLight;
        }

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

                    var player = session.AttachedEntity;

                    if (player == null)
                    {
                        break;
                    }

                    var ownerTransform = Owner.Transform;
                    var playerTransform = player.Transform;

                    if (!playerTransform.Coordinates.InRange(Owner.EntityManager, ownerTransform.Coordinates, 2) ||
                        !ownerTransform.IsMapTransform &&
                        !playerTransform.ContainsEntity(ownerTransform))
                    {
                        break;
                    }

                    var entity = Owner.EntityManager.GetEntity(remove.EntityUid);

                    if (entity == null || _storage?.Contains(entity) == false)
                    {
                        break;
                    }

                    var item = entity.GetComponent<ItemComponent>();
                    if (item == null ||
                        !player.TryGetComponent(out HandsComponent? hands))
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

                    var player = session.AttachedEntity;

                    if (player == null)
                    {
                        break;
                    }

                    if (!player.InRangeUnobstructed(Owner, popup: true))
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
            Logger.DebugS(LoggerName, $"Storage (UID {Owner.Uid}) attacked by user (UID {eventArgs.User.Uid}) with entity (UID {eventArgs.Using.Uid}).");

            if (Owner.HasComponent<PlaceableSurfaceComponent>())
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
        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            EnsureInitialCalculated();
            OpenStorageUI(eventArgs.User);
            return false;
        }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            ((IUse) this).UseEntity(new UseEntityEventArgs(eventArgs.User));
        }

        /// <summary>
        /// Allows a user to pick up entities by clicking them, or pick up all entities in a certain radius
        /// arround a click.
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (!eventArgs.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true)) return false;

            // Pick up all entities in a radius around the clicked location.
            // The last half of the if is because carpets exist and this is terrible
            if(_areaInsert && (eventArgs.Target == null || !eventArgs.Target.HasComponent<SharedItemComponent>()))
            {
                var validStorables = new List<IEntity>();
                foreach (var entity in IoCManager.Resolve<IEntityLookup>().GetEntitiesInRange(eventArgs.ClickLocation, 1))
                {
                    if (!entity.Transform.IsMapTransform
                        || entity == eventArgs.User
                        || !entity.HasComponent<SharedItemComponent>())
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
                    var result = await doAfterSystem.DoAfter(doAfterArgs);
                    if (result != DoAfterStatus.Finished) return true;
                }

                var successfullyInserted = new List<EntityUid>();
                var successfullyInsertedPositions = new List<EntityCoordinates>();
                foreach (var entity in validStorables)
                {
                    // Check again, situation may have changed for some entities, but we'll still pick up any that are valid
                    if (!entity.Transform.IsMapTransform
                        || entity == eventArgs.User
                        || !entity.HasComponent<SharedItemComponent>())
                        continue;
                    var coords = entity.Transform.Coordinates;
                    if (PlayerInsertEntityInWorld(eventArgs.User, entity))
                    {
                        successfullyInserted.Add(entity.Uid);
                        successfullyInsertedPositions.Add(coords);
                    }
                }

                // If we picked up atleast one thing, play a sound and do a cool animation!
                if (successfullyInserted.Count>0)
                {
                    PlayStorageSound();
                    SendNetworkMessage(
                        new AnimateInsertingEntitiesMessage(
                            successfullyInserted,
                            successfullyInsertedPositions
                        )
                    );
                }
                return true;
            }
            // Pick up the clicked entity
            else if(_quickInsert)
            {
                if (eventArgs.Target == null
                    || !eventArgs.Target.Transform.IsMapTransform
                    || eventArgs.Target == eventArgs.User
                    || !eventArgs.Target.HasComponent<SharedItemComponent>())
                    return false;
                var position = eventArgs.Target.Transform.Coordinates;
                if(PlayerInsertEntityInWorld(eventArgs.User, eventArgs.Target))
                {
                    SendNetworkMessage(new AnimateInsertingEntitiesMessage(
                        new List<EntityUid>() { eventArgs.Target.Uid },
                        new List<EntityCoordinates>() { position }
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

        void IExAct.OnExplosion(ExplosionEventArgs eventArgs)
        {
            if (eventArgs.Severity < ExplosionSeverity.Heavy)
            {
                return;
            }

            var storedEntities = StoredEntities?.ToList();

            if (storedEntities == null)
            {
                return;
            }

            foreach (var entity in storedEntities)
            {
                var exActs = entity.GetAllComponents<IExAct>().ToArray();
                foreach (var exAct in exActs)
                {
                    exAct.OnExplosion(eventArgs);
                }
            }
        }

        public void PlayStorageSound()
        {
            if (string.IsNullOrEmpty(StorageSoundCollection))
            {
                return;
            }

            var file = AudioHelpers.GetRandomFileFromSoundCollection(StorageSoundCollection);
            SoundSystem.Play(Filter.Pvs(Owner), file, Owner, AudioParams.Default);
        }
    }
}
