using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.GameObjects.Components.Storage;
using SS14.Server.GameObjects;
using SS14.Server.GameObjects.Components.Container;
using SS14.Server.Interfaces.Player;
using SS14.Server.Player;
using SS14.Shared.Enums;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Serialization;
using System.Collections.Generic;

namespace Content.Server.GameObjects
{
    /// <summary>
    /// Storage component for containing entities within this one, matches a UI on the client which shows stored entities
    /// </summary>
    public class ServerStorageComponent : SharedStorageComponent, IAttackby, IUse
    {
        private Container storage;

        private int StorageUsed = 0;
        private int StorageCapacityMax = 10000;
        public HashSet<IPlayerSession> SubscribedSessions = new HashSet<IPlayerSession>();

        public override void OnAdd()
        {
            base.OnAdd();

            storage = ContainerManagerComponent.Create<Container>("storagebase", Owner);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref StorageCapacityMax, "Capacity", 10000);
        }

        /// <summary>
        /// Removes from the storage container and updates the stored value
        /// </summary>
        /// <param name="toremove"></param>
        /// <returns></returns>
        bool Remove(IEntity toremove)
        {
            if (storage.Remove(toremove))
            {
                Logger.InfoS("Storage", "Storage (UID {0}) had entity (UID {1}) removed from it.", Owner.Uid, toremove.Uid);
                StorageUsed -= toremove.GetComponent<StoreableComponent>().ObjectSize;
                UpdateClientInventories();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Inserts into the storage container
        /// </summary>
        /// <param name="toinsert"></param>
        /// <returns></returns>
        bool Insert(IEntity toinsert)
        {
            if (CanInsert(toinsert) && storage.Insert(toinsert))
            {
                Logger.InfoS("Storage", "Storage (UID {0}) had entity (UID {1}) inserted into it.", Owner.Uid, toinsert.Uid);
                StorageUsed += toinsert.GetComponent<StoreableComponent>().ObjectSize;
                UpdateClientInventories();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Verifies the object can be inserted by checking if it is storeable and if it keeps under the capacity limit
        /// </summary>
        /// <param name="toinsert"></param>
        /// <returns></returns>
        bool CanInsert(IEntity toinsert)
        {
            if (toinsert.TryGetComponent(out StoreableComponent store))
            {
                if (store.ObjectSize <= (StorageCapacityMax - StorageUsed))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Inserts storeable entities into this storage container if possible, otherwise return to the hand of the user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="attackwith"></param>
        /// <returns></returns>
        bool IAttackby.Attackby(IEntity user, IEntity attackwith)
        {
            Logger.DebugS("Storage", "Storage (UID {0}) attacked by user (UID {1}) with entity (UID {2}).", Owner.Uid, user.Uid, attackwith.Uid);
            var hands = user.GetComponent<HandsComponent>();
            //Check that we can drop the item from our hands first otherwise we obviously cant put it inside
            if (hands.Drop(hands.ActiveIndex))
            {
                var inserted = Insert(attackwith);
                if (inserted)
                {
                    return true;
                }
                else
                {
                    //Return the object to the hand since its too big or something like that
                    hands.PutInHand(attackwith.GetComponent<ItemComponent>());
                }
            }
            return false;
        }

        /// <summary>
        /// Sends a message to open the storage UI
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        bool IUse.UseEntity(IEntity user)
        {
            var user_session = user.GetComponent<BasicActorComponent>().playerSession;
            Logger.DebugS("Storage", "Storage (UID {0}) \"used\" by player session (UID {1}).", Owner.Uid, user_session.AttachedEntityUid);
            SubscribeSession(user_session);
            SendNetworkMessage(new OpenStorageUIMessage(), user_session.ConnectedClient);
            UpdateClientInventory(user_session);
            return false;
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
            if (!SubscribedSessions.Contains(session))
            {
                Logger.DebugS("Storage", "Storage (UID {0}) subscribed player session (UID {1}).", Owner.Uid, session.AttachedEntityUid);
                session.PlayerStatusChanged += HandlePlayerSessionChangeEvent;
                SubscribedSessions.Add(session);
            }
        }

        /// <summary>
        /// Removes actor from the update list.
        /// </summary>
        /// <param name="channel"></param>
        public void UnsubscribeSession(IPlayerSession session)
        {
            Logger.DebugS("Storage", "Storage (UID {0}) unsubscribed player session (UID {1}).", Owner.Uid, session.AttachedEntityUid);
            SubscribedSessions.Remove(session);
            SendNetworkMessage(new CloseStorageUIMessage(), session.ConnectedClient);
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
        /// <param name="netChannel"></param>
        /// <param name="component"></param>
        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

            switch (message)
            {
                case RemoveEntityMessage msg:
                    var playerMan = IoCManager.Resolve<IPlayerManager>();
                    var session = playerMan.GetSessionByChannel(netChannel);
                    var playerentity = session.AttachedEntity;

                    var ourtransform = Owner.GetComponent<TransformComponent>();
                    var playertransform = playerentity.GetComponent<TransformComponent>();

                    if (playertransform.LocalPosition.InRange(ourtransform.LocalPosition, 2)
                        && (ourtransform.IsMapTransform || playertransform.ContainsEntity(ourtransform)))
                    {
                        var remove = (RemoveEntityMessage)message;
                        var entity = IoCManager.Resolve<IEntityManager>().GetEntity(remove.EntityUid);
                        if (entity != null && storage.Contains(entity))
                        {
                            Remove(entity);

                            var item = entity.GetComponent<ItemComponent>();
                            if (item != null && playerentity.TryGetComponent(out HandsComponent hands))
                            {
                                if (hands.PutInHand(item))
                                    return;
                            }

                            entity.GetComponent<TransformComponent>().WorldPosition = Owner.GetComponent<TransformComponent>().WorldPosition;
                        }
                    }
                    break;
            }
        }
    }
}
