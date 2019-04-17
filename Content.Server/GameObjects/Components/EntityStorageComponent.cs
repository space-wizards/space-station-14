using Content.Server.GameObjects.EntitySystems;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
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

        public override void Initialize()
        {
            base.Initialize();
            Contents = ContainerManagerComponent.Ensure<Container>($"{typeof(EntityStorageComponent).FullName}{Owner.Uid.ToString()}", Owner);
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
                if (!AddToContents(entity))
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

    private bool AddToContents(IEntity entity)
        {
            var collidableComponent = Owner.GetComponent<ICollidableComponent>();
            if(entity.TryGetComponent<ICollidableComponent>(out var entityCollidableComponent))
            {
                if(collidableComponent.WorldAABB.Size.X < entityCollidableComponent.WorldAABB.Size.X
                    || collidableComponent.WorldAABB.Size.Y < entityCollidableComponent.WorldAABB.Size.Y)
                {
                    return false;
                }

                if (collidableComponent.WorldAABB.Left > entityCollidableComponent.WorldAABB.Left)
                {
                    entity.Transform.WorldPosition += new Vector2(collidableComponent.WorldAABB.Left - entityCollidableComponent.WorldAABB.Left, 0);
                }
                else if (collidableComponent.WorldAABB.Right < entityCollidableComponent.WorldAABB.Right)
                {
                    entity.Transform.WorldPosition += new Vector2(collidableComponent.WorldAABB.Right - entityCollidableComponent.WorldAABB.Right, 0);
                }
                if (collidableComponent.WorldAABB.Bottom > entityCollidableComponent.WorldAABB.Bottom)
                {
                    entity.Transform.WorldPosition += new Vector2(0, collidableComponent.WorldAABB.Bottom - entityCollidableComponent.WorldAABB.Bottom);
                }
                else if (collidableComponent.WorldAABB.Top < entityCollidableComponent.WorldAABB.Top)
                {
                    entity.Transform.WorldPosition += new Vector2(0, collidableComponent.WorldAABB.Top - entityCollidableComponent.WorldAABB.Top);
                }
            }
            if (Contents.CanInsert(entity))
            {
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
                Contents.Remove(containedEntity);
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
