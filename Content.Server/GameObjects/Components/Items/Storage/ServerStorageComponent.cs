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
            if(storage.Remove(toremove))
            {
                StorageUsed -= toremove.GetComponent<StoreableComponent>().ObjectSize;
                UpdateClientInventory();
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
            if(CanInsert(toinsert) && storage.Insert(toinsert))
            {
                StorageUsed += toinsert.GetComponent<StoreableComponent>().ObjectSize;
                UpdateClientInventory();
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
            if(toinsert.TryGetComponent(out StoreableComponent store))
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
            if(hands.Drop(hands.ActiveIndex))
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
            SendNetworkMessage(new OpenStorageUIMessage());
            return false;
        }

        /// <summary>
        /// Updates the storage UI on all clients telling them of the entities stored in this container
        /// </summary>
        private void UpdateClientInventory()
        {
            Dictionary<EntityUid, int> storedentities = new Dictionary<EntityUid, int>();
            foreach (var entities in storage.ContainedEntities)
            {
                storedentities.Add(entities.Uid, entities.GetComponent<StoreableComponent>().ObjectSize);
            }
            SendNetworkMessage(new StorageHeldItemsMessage(storedentities, StorageUsed, StorageCapacityMax));
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
                            UpdateClientInventory();

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
