#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects.Components.Items;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Storage;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystemMessages;
using Robust.Server.Interfaces.Player;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Items.Storage
{
    /// <summary>
    /// Storage component for containing entities within this one, matches a UI on the client which shows stored entities
    /// </summary>
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IStorageComponent))]
    public class ServerStorageComponent : SharedStorageComponent, IInteractUsing, IUse, IActivate, IStorageComponent, IDestroyAct, IExAct,
        IDragDrop
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;

        private const string LoggerName = "Storage";

        private Container? _storage;

        private bool _occludesLight;
        private bool _storageInitialCalculated;
        private int _storageUsed;
        private int _storageCapacityMax;
        public readonly HashSet<IPlayerSession> SubscribedSessions = new HashSet<IPlayerSession>();

        [ViewVariables]
        public IReadOnlyCollection<IEntity>? StoredEntities => _storage?.ContainedEntities;

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
                var item = entity.GetComponent<StorableComponent>();
                _storageUsed += item.ObjectSize;
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

            if (entity.TryGetComponent(out StorableComponent? store) &&
                store.ObjectSize > _storageCapacityMax - _storageUsed)
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

        /// <summary>
        ///     Removes from the storage container and updates the stored value
        /// </summary>
        /// <param name="entity">The entity to remove</param>
        /// <returns>true if no longer in storage, false otherwise</returns>
        public bool Remove(IEntity entity)
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

            EnsureInitialCalculated();

            Logger.DebugS(LoggerName, $"Storage (UID {Owner.Uid}) had entity (UID {message.Entity.Uid}) inserted into it.");

            _storageUsed += message.Entity.GetComponent<StorableComponent>().ObjectSize;

            UpdateClientInventories();
        }

        public void HandleEntityMaybeRemoved(EntRemovedFromContainerMessage message)
        {
            if (message.Container != _storage)
            {
                return;
            }

            EnsureInitialCalculated();

            Logger.DebugS(LoggerName, $"Storage (UID {Owner.Uid}) had entity (UID {message.Entity.Uid}) removed from it.");

            if (!message.Entity.TryGetComponent(out StorableComponent? storable))
            {
                Logger.WarningS(LoggerName, $"Removed entity {message.Entity.Uid} without a StorableComponent from storage {Owner.Uid} at {Owner.Transform.MapPosition}");

                RecalculateStorageUsed();
                return;
            }

            _storageUsed -= storable.ObjectSize;

            UpdateClientInventories();
        }

        /// <summary>
        ///     Inserts an entity into storage from the player's active hand
        /// </summary>
        /// <param name="player">The player to insert an entity from</param>
        /// <returns>true if inserted, false otherwise</returns>
        public bool PlayerInsertEntity(IEntity player)
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
        ///     Opens the storage UI for an entity
        /// </summary>
        /// <param name="entity">The entity to open the UI for</param>
        public void OpenStorageUI(IEntity entity)
        {
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

            var storedEntities = new Dictionary<EntityUid, int>();

            foreach (var entities in _storage.ContainedEntities)
            {
                storedEntities.Add(entities.Uid, entities.GetComponent<StorableComponent>().ObjectSize);
            }

            SendNetworkMessage(new StorageHeldItemsMessage(storedEntities, _storageUsed, _storageCapacityMax), session.ConnectedClient);
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
            _storage = ContainerManagerComponent.Ensure<Container>("storagebase", Owner);
            _storage.OccludesLight = _occludesLight;
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _storageCapacityMax, "capacity", 10000);
            serializer.DataField(ref _occludesLight, "occludesLight", true);
            //serializer.DataField(ref StorageUsed, "used", 0);
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

                    if (!playerTransform.Coordinates.InRange(_entityManager, ownerTransform.Coordinates, 2) ||
                        !ownerTransform.IsMapTransform &&
                        !playerTransform.ContainsEntity(ownerTransform))
                    {
                        break;
                    }

                    var entity = _entityManager.GetEntity(remove.EntityUid);

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

                    PlayerInsertEntity(player);

                    break;
                }
                case CloseStorageUIMessage _:
                {
                    if (!(session is IPlayerSession playerSession))
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

            return PlayerInsertEntity(eventArgs.User);
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
            ((IUse) this).UseEntity(new UseEntityEventArgs { User = eventArgs.User });
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

        bool IDragDrop.CanDragDrop(DragDropEventArgs eventArgs)
        {
            return eventArgs.Target.TryGetComponent(out PlaceableSurfaceComponent? placeable) &&
                   placeable.IsPlaceable;
        }

        bool IDragDrop.DragDrop(DragDropEventArgs eventArgs)
        {
            if (!ActionBlockerSystem.CanInteract(eventArgs.User))
            {
                return false;
            }

            if (!eventArgs.Target.TryGetComponent<PlaceableSurfaceComponent>(out var placeableSurface) ||
                !placeableSurface.IsPlaceable)
            {
                return false;
            }

            var storedEntities = StoredEntities?.ToList();

            if (storedEntities == null)
            {
                return false;
            }

            // empty everything out
            foreach (var storedEntity in StoredEntities.ToList())
            {
                if (Remove(storedEntity))
                {
                    storedEntity.Transform.WorldPosition = eventArgs.DropLocation.Position;
                }
            }

            return true;
        }
    }
}
