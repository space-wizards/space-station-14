using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Storage;
using SS14.Server.GameObjects.Components.Container;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Serialization;
using SS14.Shared.ViewVariables;
using System;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components
{
    public class EntityStorageComponent : Component, IAttackHand
    {
        public override string Name => "EntityStorage";

        private ServerStorageComponent StorageComponent;
        private int StorageCapacityMax;
        private bool IsCollidableWhenOpen;
        private List<ContainerSlot> Contents;
        private IEntityQuery entityQuery;

        public override void Initialize()
        {
            base.Initialize();
            Contents = new List<ContainerSlot>();
            for (int i = 0; i < StorageCapacityMax; i++)
            {
                Contents.Add(ContainerManagerComponent.Ensure<ContainerSlot>($"{typeof(EntityStorageComponent).FullName}{i}{Owner.Uid.ToString()}", Owner));
            }
            StorageComponent = Owner.AddComponent<ServerStorageComponent>();
            StorageComponent.Initialize();
            entityQuery = new IntersectingEntityQuery(Owner);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref StorageCapacityMax, "Capacity", 30);
            serializer.DataField(ref IsCollidableWhenOpen, "IsCollidableWhenOpen", false);
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Open
        {
            get => StorageComponent.Open;
            set => StorageComponent.Open = value;
        }

        public bool AttackHand(AttackHandEventArgs eventArgs)
        {
            if (Open)
            {
                CloseStorage();
            }
            else
            {
                OpenStorage();
            }
            return true;
        }

        private void CloseStorage()
        {
            Open = false;
            var entities = Owner.EntityManager.GetEntities(entityQuery);
            int count = 0;
            foreach (var entity in entities)
            {
                if (!AddToContents(entity, count))
                {
                    continue;
                }
                count++;
                if (count >= StorageCapacityMax)
                {
                    break;
                }
            }
            ModifyComponents();
        }

        private void OpenStorage()
        {
            Open = true;
            EmptyContents();
            ModifyComponents();
        }

        private void ModifyComponents()
        { 
            if (Owner.TryGetComponent<ICollidableComponent>(out var collidableComponent))
            {
                collidableComponent.CollisionEnabled = IsCollidableWhenOpen || !Open;
            }
            if (Owner.TryGetComponent<PlaceableSurfaceComponent>(out var placeableSurfaceComponent))
            {
                placeableSurfaceComponent.IsPlaceable = Open;
            }
        }

    private bool AddToContents(IEntity entity, int index)
        {
            if(Contents[index].CanInsert(entity))
            {
                Contents[index].Insert(entity);
                return true;
            }
            return false;
        }

        private void EmptyContents()
        {
            foreach (var containerSlot in Contents)
            {
                containerSlot.Remove(containerSlot.ContainedEntity);
            }
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

            switch (message)
            {
                case RelayMovementEntityMessage msg:
                    if(msg.Entity.TryGetComponent<HandsComponent>(out var handsComponent))
                    {
                        OpenStorage();
                    }
                    break;
            }
        }
    }
}
