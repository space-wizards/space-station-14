using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.Interfaces.GameObjects;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Storage;
using Content.Shared.Interfaces;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystemMessages;
using Robust.Server.Interfaces.Player;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Players;
using Robust.Shared.Serialization;

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
#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IEntityManager _entityManager;
#pragma warning restore 649

        private Container _storage;

        private bool _storageInitialCalculated;
        private int _storageUsed;
        private int _storageCapacityMax = 10000;
        public readonly HashSet<IPlayerSession> SubscribedSessions = new HashSet<IPlayerSession>();

        public IReadOnlyCollection<IEntity> StoredEntities => _storage.ContainedEntities;

        public override void Initialize()
        {
            base.Initialize();

            _storage = ContainerManagerComponent.Ensure<Container>("storagebase", Owner);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _storageCapacityMax, "Capacity", 10000);
            //serializer.DataField(ref StorageUsed, "used", 0);
        }

        /// <summary>
        /// Removes from the storage container and updates the stored value
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool Remove(IEntity entity)
        {
            _ensureInitialCalculated();
            return _storage.Remove(entity);
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

        internal void HandleEntityMaybeRemoved(EntRemovedFromContainerMessage message)
        {
            if (message.Container != _storage)
            {
                return;
            }

            _ensureInitialCalculated();

            Logger.DebugS("Storage", "Storage (UID {0}) had entity (UID {1}) removed from it.", Owner.Uid,
                message.Entity.Uid);

            if (!message.Entity.TryGetComponent(out StorableComponent storable))
            {
                RecalculateStorageUsed();
                return;
            }

            _storageUsed -= storable.ObjectSize;

            UpdateClientInventories();
        }

        /// <summary>
        /// Inserts into the storage container
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool Insert(IEntity entity)
        {
            return CanInsert(entity) && _storage.Insert(entity);
        }

        internal void HandleEntityMaybeInserted(EntInsertedIntoContainerMessage message)
        {
            if (message.Container != _storage)
            {
                return;
            }

            _ensureInitialCalculated();
            Logger.DebugS("Storage", "Storage (UID {0}) had entity (UID {1}) inserted into it.", Owner.Uid,
                message.Entity.Uid);
            _storageUsed += message.Entity.GetComponent<StorableComponent>().ObjectSize;
            UpdateClientInventories();
        }

        /// <summary>
        /// Verifies the object can be inserted by checking if it is storable and if it keeps under the capacity limit
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool CanInsert(IEntity entity)
        {
            _ensureInitialCalculated();

            if (entity.TryGetComponent(out ServerStorageComponent storage) &&
                storage._storageCapacityMax >= _storageCapacityMax)
            {
                return false;
            }

            if (entity.TryGetComponent(out StorableComponent store) &&
                store.ObjectSize > _storageCapacityMax - _storageUsed)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Inserts storable entities into this storage container if possible, otherwise return to the hand of the user
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <returns></returns>
        public bool InteractUsing(InteractUsingEventArgs eventArgs)
        {
            Logger.DebugS("Storage", "Storage (UID {0}) attacked by user (UID {1}) with entity (UID {2}).", Owner.Uid, eventArgs.User.Uid, eventArgs.Using.Uid);

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
            _ensureInitialCalculated();
            OpenStorageUI(eventArgs.User);
            return false;
        }

        public void OpenStorageUI(IEntity entity)
        {
            _ensureInitialCalculated();

            var userSession = entity.GetComponent<BasicActorComponent>().playerSession;

            Logger.DebugS("Storage", "Storage (UID {0}) \"used\" by player session (UID {1}).", Owner.Uid, userSession.AttachedEntityUid);

            SubscribeSession(userSession);
            SendNetworkMessage(new OpenStorageUIMessage(), userSession.ConnectedClient);
            UpdateClientInventory(userSession);
        }

        /// <summary>
        /// Updates the storage UI on all subscribed actors, informing them of the state of the container.
        /// </summary>
        private void UpdateClientInventories()
        {
            foreach (var session in SubscribedSessions)
            {
                UpdateClientInventory(session);
            }
        }

        /// <summary>
        /// Adds a session to the update list.
        /// </summary>
        /// <param name="session">The session to add</param>
        private void SubscribeSession(IPlayerSession session)
        {
            _ensureInitialCalculated();

            if (!SubscribedSessions.Contains(session))
            {
                Logger.DebugS("Storage", "Storage (UID {0}) subscribed player session (UID {1}).", Owner.Uid, session.AttachedEntityUid);

                session.PlayerStatusChanged += HandlePlayerSessionChangeEvent;
                SubscribedSessions.Add(session);
                UpdateDoorState();
            }
        }

        /// <summary>
        /// Removes a session from the update list.
        /// </summary>
        /// <param name="session">The session to remove</param>
        public void UnsubscribeSession(IPlayerSession session)
        {
            if (SubscribedSessions.Contains(session))
            {
                Logger.DebugS("Storage", "Storage (UID {0}) unsubscribed player session (UID {1}).", Owner.Uid, session.AttachedEntityUid);

                SubscribedSessions.Remove(session);
                SendNetworkMessage(new CloseStorageUIMessage(), session.ConnectedClient);
                UpdateDoorState();
            }
        }

        private void UpdateDoorState()
        {
            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(StorageVisuals.Open, SubscribedSessions.Count != 0);
            }
        }

        private void HandlePlayerSessionChangeEvent(object obj, SessionStatusEventArgs sessionStatus)
        {
            Logger.DebugS("Storage", "Storage (UID {0}) handled a status change in player session (UID {1}).", Owner.Uid, sessionStatus.Session.AttachedEntityUid);

            if (sessionStatus.NewStatus != SessionStatus.InGame)
            {
                UnsubscribeSession(sessionStatus.Session);
            }
        }

        /// <summary>
        /// Updates storage UI on a client, informing them of the state of the container.
        /// </summary>
        private void UpdateClientInventory(IPlayerSession session)
        {
            if (session.AttachedEntity == null)
            {
                Logger.DebugS("Storage", "Storage (UID {0}) detected no attached entity in player session (UID {1}).", Owner.Uid, session.AttachedEntityUid);

                UnsubscribeSession(session);
                return;
            }

            var storedEntities = new Dictionary<EntityUid, int>();

            foreach (var entities in _storage.ContainedEntities)
            {
                storedEntities.Add(entities.Uid, entities.GetComponent<StorableComponent>().ObjectSize);
            }

            SendNetworkMessage(new StorageHeldItemsMessage(storedEntities, _storageUsed, _storageCapacityMax), session.ConnectedClient);
        }

        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession session = null)
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
                    _ensureInitialCalculated();

                    var player = session.AttachedEntity;

                    if (player == null)
                    {
                        return;
                    }

                    var ownerTransform = Owner.GetComponent<ITransformComponent>();
                    var playerTransform = player.GetComponent<ITransformComponent>();

                    if (!playerTransform.GridPosition.InRange(_mapManager, ownerTransform.GridPosition, 2) ||
                        !ownerTransform.IsMapTransform &&
                        !playerTransform.ContainsEntity(ownerTransform))
                    {
                        return;
                    }

                    var entity = _entityManager.GetEntity(remove.EntityUid);

                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    if (entity == null || !_storage.Contains(entity))
                    {
                        return;
                    }

                    var item = entity.GetComponent<ItemComponent>();
                    if (item == null ||
                        !player.TryGetComponent(out HandsComponent hands))
                    {
                        return;
                    }

                    if (hands.CanPutInHand(item))
                    {
                        return;
                    }

                    hands.PutInHand(item);

                    break;
                }
                case InsertEntityMessage _:
                {
                    _ensureInitialCalculated();

                    var player = session.AttachedEntity;

                    if (player == null)
                    {
                        return;
                    }

                    var storagePosition = Owner.Transform.MapPosition;

                    if (!InteractionChecks.InRangeUnobstructed(player, storagePosition))
                    {
                        return;
                    }

                    PlayerInsertEntity(player);

                    break;
                }
                case CloseStorageUIMessage _:
                {
                    UnsubscribeSession(session as IPlayerSession);
                    break;
                }
            }
        }

        /// <inheritdoc />
        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            ((IUse) this).UseEntity(new UseEntityEventArgs { User = eventArgs.User });
        }

        private void _ensureInitialCalculated()
        {
            if (_storageInitialCalculated)
            {
                return;
            }

            RecalculateStorageUsed();

            _storageInitialCalculated = true;
        }

        void IDestroyAct.OnDestroy(DestructionEventArgs eventArgs)
        {
            var storedEntities = _storage.ContainedEntities.ToList();
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

            var storedEntities = _storage.ContainedEntities.ToList();
            foreach (var entity in storedEntities)
            {
                var exActs = entity.GetAllComponents<IExAct>();
                foreach (var exAct in exActs)
                {
                    exAct.OnExplosion(eventArgs);
                }
            }
        }

        /// <summary>
        /// Inserts an entity into the storage component from the players active hand.
        /// </summary>
        public bool PlayerInsertEntity(IEntity player)
        {
            _ensureInitialCalculated();

            if (!player.TryGetComponent(out IHandsComponent hands) || hands.GetActiveHand == null)
            {
                return false;
            }

            var toInsert = hands.GetActiveHand;

            if (hands.Drop(toInsert.Owner))
            {
                if (Insert(toInsert.Owner))
                {
                    return true;
                }
                else
                {
                    hands.PutInHand(toInsert);
                }
            }

            Owner.PopupMessage(player, "Can't insert.");
            return false;
        }

        public bool DragDrop(DragDropEventArgs eventArgs)
        {
            if (!eventArgs.Target.TryGetComponent<PlaceableSurfaceComponent>(out var placeableSurface) ||
                !placeableSurface.IsPlaceable)
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
