using System.Linq;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Sound;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Storage;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IStorageComponent))]
    public class EntityStorageComponent : Component, IActivate, IStorageComponent
    {
        public override string Name => "EntityStorage";

        private const float MaxSize = 1.0f; // maximum width or height of an entity allowed inside the storage.

        private int StorageCapacityMax;
        private bool IsCollidableWhenOpen;
        private Container Contents;
        private IEntityQuery entityQuery;
        private bool _locked;
        private bool _showContents;

        /// <summary>
        /// Determines if the storage is locked, meaning it cannot be opened.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool Locked
        {
            get => _locked;
            set => _locked = value;
        }

        /// <summary>
        /// Determines if the container contents should be drawn when the container is closed.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool ShowContents
        {
            get => _showContents;
            set
            {
                _showContents = value;
                Contents.ShowContents = _showContents;
            }
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            Contents = ContainerManagerComponent.Ensure<Container>(nameof(EntityStorageComponent), Owner);
            entityQuery = new IntersectingEntityQuery(Owner);

            Contents.ShowContents = _showContents;
        }

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref StorageCapacityMax, "Capacity", 30);
            serializer.DataField(ref IsCollidableWhenOpen, "IsCollidableWhenOpen", false);
            serializer.DataField(ref _locked, "locked", false);
            serializer.DataField(ref _showContents, "showContents", false);
        }

        [ViewVariables]
        public bool Open { get; private set; }

        void IActivate.Activate(ActivateEventArgs eventArgs)
        {
            ToggleOpen();
        }

        private void ToggleOpen()
        {
            if (Open)
            {
                CloseStorage();
            }
            else
            {
                OpenStorage();
            }
        }

        private void ToggleLock()
        {
            _locked = !_locked;

            if (Owner.TryGetComponent(out SoundComponent soundComponent))
                soundComponent.Play(_locked ? "/Audio/machines/lockenable.ogg" : "/Audio/machines/lockreset.ogg");
        }

        private void CloseStorage()
        {
            Open = false;
            var entities = Owner.EntityManager.GetEntities(entityQuery);
            var count = 0;
            foreach (var entity in entities)
            {
                // prevents taking items out of inventories, out of containers, and orphaning child entities
                if(!entity.Transform.IsMapTransform)
                    continue;

                // only items that can be stored in an inventory, or a player, can be eaten by a locker
                if(!entity.HasComponent<StoreableComponent>() && !entity.HasComponent<IActorComponent>())
                    continue;
                
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
            if (Owner.TryGetComponent(out SoundComponent soundComponent))
            {
                soundComponent.Play("/Audio/machines/closetclose.ogg");
            }
        }

        private void OpenStorage()
        {
            if (_locked)
                return;

            Open = true;
            EmptyContents();
            ModifyComponents();
            if (Owner.TryGetComponent(out SoundComponent soundComponent))
            {
                soundComponent.Play("/Audio/machines/closetopen.ogg");
            }
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

            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(StorageVisuals.Open, Open);
            }
        }

        private bool AddToContents(IEntity entity)
        {
            var collidableComponent = Owner.GetComponent<ICollidableComponent>();
            if(entity.TryGetComponent<ICollidableComponent>(out var entityCollidableComponent))
            {
                if(MaxSize < entityCollidableComponent.WorldAABB.Size.X
                    || MaxSize < entityCollidableComponent.WorldAABB.Size.Y)
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
                // Because Insert sets the local position to (0,0), and we want to keep the contents spread out,
                // we re-apply the world position after inserting.
                var worldPos = entity.Transform.WorldPosition;
                Contents.Insert(entity);
                entity.Transform.WorldPosition = worldPos;
                return true;
            }
            return false;
        }

        private void EmptyContents()
        {
            foreach (var contained in Contents.ContainedEntities.ToArray())
            {
                Contents.Remove(contained);
            }
        }

        /// <inheritdoc />
        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

            switch (message)
            {
                case RelayMovementEntityMessage msg:
                    if (msg.Entity.HasComponent<HandsComponent>())
                    {
                        OpenStorage();
                    }
                    break;
            }
        }

        /// <inheritdoc />
        public bool Remove(IEntity entity)
        {
            return Contents.CanRemove(entity);
        }

        /// <inheritdoc />
        public bool Insert(IEntity entity)
        {
            // Trying to add while open just dumps it on the ground below us.
            if (Open)
            {
                entity.Transform.WorldPosition = Owner.Transform.WorldPosition;
                return true;
            }

            return Contents.Insert(entity);
        }

        /// <inheritdoc />
        public bool CanInsert(IEntity entity)
        {
            if (Open)
            {
                return true;
            }

            if (Contents.ContainedEntities.Count >= StorageCapacityMax)
            {
                return false;
            }

            return Contents.CanInsert(entity);
        }

        /// <summary>
        /// Adds a verb that toggles the lock of the storage.
        /// </summary>
        [Verb]
        private sealed class LockToggleVerb : Verb<EntityStorageComponent>
        {
            /// <inheritdoc />
            protected override string GetText(IEntity user, EntityStorageComponent component)
            {
                return component._locked ? "Unlock" : "Lock";
            }

            /// <inheritdoc />
            protected override VerbVisibility GetVisibility(IEntity user, EntityStorageComponent component)
            {
                return VerbVisibility.Visible;
            }

            /// <inheritdoc />
            protected override void Activate(IEntity user, EntityStorageComponent component)
            {
                component.ToggleLock();
            }
        }

        [Verb]
        private sealed class OpenToggleVerb : Verb<EntityStorageComponent>
        {
            /// <inheritdoc />
            protected override string GetText(IEntity user, EntityStorageComponent component)
            {
                return component.Open ? "Close" : "Open";
            }

            /// <inheritdoc />
            protected override VerbVisibility GetVisibility(IEntity user, EntityStorageComponent component)
            {
                return VerbVisibility.Visible;
            }

            /// <inheritdoc />
            protected override void Activate(IEntity user, EntityStorageComponent component)
            {
                component.ToggleOpen();
            }
        }
    }
}
