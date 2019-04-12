using Content.Server.GameObjects.EntitySystems;
using SS14.Server.GameObjects.Components.Container;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Map;
using SS14.Shared.Serialization;
using SS14.Shared.ViewVariables;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.GameObjects.Components
{
    public class EntityStorageComponent : Component, IAttackHand
    {
        public override string Name => "EntityStorage";

        private ServerStorageComponent StorageComponent;
        private int StorageCapacityMax;
        private bool IsCollidableWhenOpen;
        private Container Contents;
        private IEntityQuery entityQuery;
        private Dictionary<EntityUid, GridCoordinates> EntityPositionOnEntry;

        public override void Initialize()
        {
            base.Initialize();
            Contents = ContainerManagerComponent.Ensure<Container>($"{typeof(EntityStorageComponent).FullName}{Owner.Uid.ToString()}", Owner);
            StorageComponent = Owner.AddComponent<ServerStorageComponent>();
            StorageComponent.Initialize();
            entityQuery = new IntersectingEntityQuery(Owner);
            EntityPositionOnEntry = new Dictionary<EntityUid, GridCoordinates>();
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
            if(Contents.CanInsert(entity))
            {
                EntityPositionOnEntry[entity.Uid] = entity.Transform.GridPosition;
                Contents.Insert(entity);
                return true;
            }
            return false;
        }

        private void EmptyContents()
        {
            while (Contents.ContainedEntities.Count > 0 )
            {
                var containedEntity = Contents.ContainedEntities.First();
                if (Contents.Remove(containedEntity))
                {
                    containedEntity.Transform.GridPosition = EntityPositionOnEntry[containedEntity.Uid];
                }
            }
            EntityPositionOnEntry.Clear();
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
