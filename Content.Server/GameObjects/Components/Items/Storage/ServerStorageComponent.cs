using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.GameObjects.Components.Storage;
using SS14.Server.GameObjects;
using SS14.Server.GameObjects.Components.Container;
using SS14.Server.Interfaces.Player;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Serialization;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Log;
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
        public HashSet<INetChannel> SubscribedChannels = new HashSet<INetChannel>();

        public ServerStorageComponent()
        {
            var EntitySystemManager = IoCManager.Resolve<IEntitySystemManager>();
            var storageSystem = EntitySystemManager.GetEntitySystem<StorageSystem>();
            storageSystem.StoringComponents.Add(this);
        }

        ~ServerStorageComponent()
        {
            var EntitySystemManager = IoCManager.Resolve<IEntitySystemManager>();
            var storageSystem = EntitySystemManager.GetEntitySystem<StorageSystem>();
            storageSystem.StoringComponents.Remove(this);
        }

        public override void OnAdd()
        {
            base.OnAdd();

            storage = ContainerManagerComponent.Create<Container>("storagebase", Owner);
        }

        public override void ExposeData(EntitySerializer serializer)
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
            var user_channel = user.GetComponent<BasicActorComponent>().playerSession.ConnectedClient;
            SubscribedChannels.Add(user_channel);
            SendNetworkMessage(new OpenStorageUIMessage(), user_channel);
            UpdateClientInventory(user_channel);
            return false;
        }

        /// <summary>
        /// Updates the storage UI on all subscribed clients, informing them of the state of the container.
        /// </summary>
        private void UpdateClientInventories()
        {
            foreach (INetChannel channel in SubscribedChannels)
            {
                UpdateClientInventory(channel);
            }
        }

        /// <summary>
        /// Stops a channel from receiving updates.
        /// </summary>
        /// <param name="channel"></param>
        public void UnsubscribeChannel(INetChannel channel)
        {
            SubscribedChannels.Remove(channel);
            SendNetworkMessage(new CloseStorageUIMessage(), channel);
        }

        /// <summary>
        /// Unsubscribes all subscribed channels that are no longer allowed to see this storage.
        /// E.g actors who are too far away from this storage.
        /// </summary>
        public void ValidateChannels()
        {
            foreach (INetChannel channel in SubscribedChannels)
            {
                var PlayerManager = IoCManager.Resolve<IPlayerManager>();
                var PlayerSession = PlayerManager.GetSessionByChannel(channel);
                var Player = PlayerSession.AttachedEntity;
                if (Player.HasComponent<TransformComponent>() && Owner.HasComponent<TransformComponent>())
                {
                    var player_transform = Player.GetComponent<TransformComponent>();
                    var owner_transform = Player.GetComponent<TransformComponent>();
                    if (player_transform.MapID != owner_transform.MapID ||
                        (player_transform.WorldPosition - owner_transform.WorldPosition).Length > 2) //Todo: replace with player's "reach"
                    {
                        UnsubscribeChannel(channel);
                    }
                }
            }
        }

        /// <summary>
        /// Updates storage UI on a client, informing them of the state of the container.
        /// </summary>
        private void UpdateClientInventory(INetChannel channel)
        {
            Dictionary<EntityUid, int> storedentities = new Dictionary<EntityUid, int>();
            foreach (var entities in storage.ContainedEntities)
            {
                storedentities.Add(entities.Uid, entities.GetComponent<StoreableComponent>().ObjectSize);
            }
            SendNetworkMessage(new StorageHeldItemsMessage(storedentities, StorageUsed, StorageCapacityMax), channel);
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
