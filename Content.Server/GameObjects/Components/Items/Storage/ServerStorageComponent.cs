using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.EntitySystems.Click;
using Content.Server.Interfaces.GameObjects.Components.Interaction;
using Content.Server.Interfaces.GameObjects;
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
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects
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

        private Container storage;

        private bool _storageInitialCalculated = false;
        private int StorageUsed = 0;
        private int StorageCapacityMax = 10000;
        public HashSet<IPlayerSession> SubscribedSessions = new HashSet<IPlayerSession>();

        public IReadOnlyCollection<IEntity> StoredEntities => storage.ContainedEntities;

        public override void Initialize()
        {
            base.Initialize();

            storage = ContainerManagerComponent.Ensure<Container>("storagebase", Owner);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref StorageCapacityMax, "Capacity", 10000);
            //serializer.DataField(ref StorageUsed, "used", 0);
        }

        /// <summary>
        /// Removes from the storage container and updates the stored value
        /// </summary>
        /// <param name="toremove"></param>
        /// <returns></returns>
        public bool Remove(IEntity toremove)
        {
            _ensureInitialCalculated();
            return storage.Remove(toremove);
        }

        internal void HandleEntityMaybeRemoved(EntRemovedFromContainerMessage message)
        {
            if (message.Container != storage)
            {
                return;
            }

            _ensureInitialCalculated();
            Logger.DebugS("Storage", "Storage (UID {0}) had entity (UID {1}) removed from it.", Owner.Uid,
                message.Entity.Uid);
            StorageUsed -= message.Entity.GetComponent<StoreableComponent>().ObjectSize;
            UpdateClientInventories();
        }

        /// <summary>
        /// Inserts into the storage container
        /// </summary>
        /// <param name="toinsert"></param>
        /// <returns></returns>
        public bool Insert(IEntity toinsert)
        {
            return CanInsert(toinsert) && storage.Insert(toinsert);
        }

        internal void HandleEntityMaybeInserted(EntInsertedIntoContainerMessage message)
        {
            if (message.Container != storage)
            {
                return;
            }

            _ensureInitialCalculated();
            Logger.DebugS("Storage", "Storage (UID {0}) had entity (UID {1}) inserted into it.", Owner.Uid,
                message.Entity.Uid);
            StorageUsed += message.Entity.GetComponent<StoreableComponent>().ObjectSize;
            UpdateClientInventories();
        }

        /// <summary>
        /// Verifies the object can be inserted by checking if it is storeable and if it keeps under the capacity limit
        /// </summary>
        /// <param name="toinsert"></param>
        /// <returns></returns>
        public bool CanInsert(IEntity toinsert)
        {
            _ensureInitialCalculated();

            if (toinsert.TryGetComponent(out ServerStorageComponent storage))
            {
                if (storage.StorageCapacityMax >= StorageCapacityMax)
                    return false;
            }

            if (toinsert.TryGetComponent(out StoreableComponent store))
            {
                if (store.ObjectSize > (StorageCapacityMax - StorageUsed))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Inserts storeable entities into this storage container if possible, otherwise return to the hand of the user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="attackwith"></param>
        /// <returns></returns>
        public bool InteractUsing(InteractUsingEventArgs eventArgs)
        {
            Logger.DebugS("Storage", "Storage (UID {0}) attacked by user (UID {1}) with entity (UID {2}).", Owner.Uid, eventArgs.User.Uid, eventArgs.Using.Uid);

            if(Owner.TryGetComponent<PlaceableSurfaceComponent>(out var placeableSurfaceComponent))
            {
                return false;
            }



            return PlayerInsertEntity(eventArgs.User);
         }

        /// <summary>
        /// Sends a message to open the storage UI
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        bool IUse.UseEntity(UseEntityEventArgs eventArgs)
        {
            _ensureInitialCalculated();
            OpenStorageUI(eventArgs.User);
            return false;
        }

        public void OpenStorageUI(IEntity Character)
        {
            _ensureInitialCalculated();
            var user_session = Character.GetComponent<BasicActorComponent>().playerSession;
            Logger.DebugS("Storage", "Storage (UID {0}) \"used\" by player session (UID {1}).", Owner.Uid, user_session.AttachedEntityUid);
            SubscribeSession(user_session);
            SendNetworkMessage(new OpenStorageUIMessage(), user_session.ConnectedClient);
            UpdateClientInventory(user_session);
        }

        /// <summary>
        /// Updates the storage UI on all subscribed actors, informing them of the state of the container.
        /// </summary>
        private void UpdateClientInventories()
        {
            foreach (IPlayerSession session in SubscribedSessions)
            {
                UpdateClientInventory(session);
            }
        }

        /// <summary>
        /// Adds actor to the update list.
        /// </summary>
        /// <param name="actor"></param>
        public void SubscribeSession(IPlayerSession session)
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
        /// Removes actor from the update list.
        /// </summary>
        /// <param name="channel"></param>
        public void UnsubscribeSession(IPlayerSession session)
        {
            if(SubscribedSessions.Contains(session))
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

        public void HandlePlayerSessionChangeEvent(object obj, SessionStatusEventArgs SSEA)
        {
            Logger.DebugS("Storage", "Storage (UID {0}) handled a status change in player session (UID {1}).", Owner.Uid, SSEA.Session.AttachedEntityUid);
            if (SSEA.NewStatus != SessionStatus.InGame)
            {
                UnsubscribeSession(SSEA.Session);
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
            Dictionary<EntityUid, int> storedentities = new Dictionary<EntityUid, int>();
            foreach (var entities in storage.ContainedEntities)
            {
                storedentities.Add(entities.Uid, entities.GetComponent<StoreableComponent>().ObjectSize);
            }
            SendNetworkMessage(new StorageHeldItemsMessage(storedentities, StorageUsed, StorageCapacityMax), session.ConnectedClient);
        }

        /// <summary>
        /// Receives messages to remove entities from storage, verifies the player can do them,
        /// and puts the removed entity in hand or on the ground
        /// </summary>
        /// <param name="message"></param>
        /// <param name="channel"></param>
        /// <param name="session"></param>
        public override void HandleNetworkMessage(ComponentMessage message, INetChannel channel, ICommonSession session = null)
        {
            base.HandleNetworkMessage(message, channel, session);

            if (session == null)
            {
                throw new ArgumentException(nameof(session));
            }

            switch (message)
            {
                case RemoveEntityMessage _:
                {
                    _ensureInitialCalculated();
                    var playerentity = session.AttachedEntity;

                    var ourtransform = Owner.GetComponent<ITransformComponent>();
                    var playertransform = playerentity.GetComponent<ITransformComponent>();

                    if (playertransform.GridPosition.InRange(_mapManager, ourtransform.GridPosition, 2)
                        && (ourtransform.IsMapTransform || playertransform.ContainsEntity(ourtransform)))
                    {
                        var remove = (RemoveEntityMessage)message;
                        var entity = _entityManager.GetEntity(remove.EntityUid);
                        if (entity != null && storage.Contains(entity))
                        {

                            var item = entity.GetComponent<ItemComponent>();
                            if (item != null && playerentity.TryGetComponent(out HandsComponent hands))
                            {
                                if (hands.CanPutInHand(item) && hands.PutInHand(item))
                                    {
                                    return;
                                    }
                            }

                        }
                    }
                    break;
                }
                case InsertEntityMessage _:
                {
                    _ensureInitialCalculated();
                    var playerEntity = session.AttachedEntity;
                    var storageTransform = Owner.GetComponent<ITransformComponent>();
                    var playerTransform = playerEntity.GetComponent<ITransformComponent>();
                    // TODO: Replace by proper entity range check once it is implemented.
                    if (playerTransform.GridPosition.InRange(_mapManager,
                                                             storageTransform.GridPosition,
                                                             InteractionSystem.InteractionRange))
                    {
                        PlayerInsertEntity(playerEntity);
                    }

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

            StorageUsed = 0;

            if (storage == null)
            {
                return;
            }

            foreach (var entity in storage.ContainedEntities)
            {
                var item = entity.GetComponent<StoreableComponent>();
                StorageUsed += item.ObjectSize;
            }

            _storageInitialCalculated = true;
        }

        void IDestroyAct.OnDestroy(DestructionEventArgs eventArgs)
        {
            var storedEntities = storage.ContainedEntities.ToList();
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

            var storedEntities = storage.ContainedEntities.ToList();
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
                return false;

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
            if (eventArgs.Target.TryGetComponent<PlaceableSurfaceComponent>(out var placeableSurface))
            {
                if (!placeableSurface.IsPlaceable) return false;

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

            return false;
        }
    }
}
