#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Interactable;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.GameObjects.Components.Storage;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.Container;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Items.Storage
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IStorageComponent))]
    public class EntityStorageComponent : Component, IActivate, IStorageComponent, IInteractUsing, IDestroyAct, IActionBlocker, IExAct
    {
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        public override string Name => "EntityStorage";

        private const float MaxSize = 1.0f; // maximum width or height of an entity allowed inside the storage.

        private static readonly TimeSpan InternalOpenAttemptDelay = TimeSpan.FromSeconds(0.5);
        private TimeSpan _lastInternalOpenAttempt;

        [ViewVariables]
        private int _storageCapacityMax;
        [ViewVariables]
        private bool _isCollidableWhenOpen;
        [ViewVariables]
        protected IEntityQuery? EntityQuery;
        private bool _showContents;
        private bool _occludesLight;
        private bool _open;
        private bool _canWeldShut;
        private bool _isWeldedShut;
        private string _closeSound = "/Audio/Machines/closetclose.ogg";
        private string _openSound = "/Audio/Machines/closetopen.ogg";

        [ViewVariables]
        protected Container Contents = default!;

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

        [ViewVariables(VVAccess.ReadWrite)]
        public bool OccludesLight
        {
            get => _occludesLight;
            set
            {
                _occludesLight = value;
                Contents.OccludesLight = _occludesLight;
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool Open
        {
            get => _open;
            private set => _open = value;
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public bool IsWeldedShut
        {
            get => _isWeldedShut;
            set
            {
                _isWeldedShut = value;

                if (Owner.TryGetComponent(out AppearanceComponent? appearance))
                {
                    appearance.SetData(StorageVisuals.Welded, value);
                }
            }
        }

        private bool _beingWelded;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanWeldShut {
            get => _canWeldShut;
            set
            {
                if (_canWeldShut == value)
                    return;

                _canWeldShut = value;
                if (Owner.TryGetComponent(out AppearanceComponent? appearance))
                {
                    appearance.SetData(StorageVisuals.CanWeld, value);
                }
            }
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            Contents = ContainerManagerComponent.Ensure<Container>(nameof(EntityStorageComponent), Owner);
            EntityQuery = new IntersectingEntityQuery(Owner);

            Contents.ShowContents = _showContents;
            Contents.OccludesLight = _occludesLight;

            if (Owner.TryGetComponent<PlaceableSurfaceComponent>(out var placeableSurfaceComponent))
            {
                placeableSurfaceComponent.IsPlaceable = Open;
            }
        }

        /// <inheritdoc />
        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _storageCapacityMax, "Capacity", 30);
            serializer.DataField(ref _isCollidableWhenOpen, "IsCollidableWhenOpen", false);
            serializer.DataField(ref _showContents, "showContents", false);
            serializer.DataField(ref _occludesLight, "occludesLight", true);
            serializer.DataField(ref _open, "open", false);
            serializer.DataField(this, a => a.IsWeldedShut, "IsWeldedShut", false);
            serializer.DataField(this, a => a.CanWeldShut, "CanWeldShut", true);
            serializer.DataField(this, x => _closeSound, "closeSound", "/Audio/Machines/closetclose.ogg");
            serializer.DataField(this, x => _openSound, "openSound", "/Audio/Machines/closetopen.ogg");
        }

        public virtual void Activate(ActivateEventArgs eventArgs)
        {
            ToggleOpen(eventArgs.User);
        }

        public virtual bool CanOpen(IEntity user, bool silent = false)
        {
            if (IsWeldedShut)
            {
                if(!silent) Owner.PopupMessage(user, Loc.GetString("It's welded completely shut!"));
                return false;
            }
            return true;
        }

        public virtual bool CanClose(IEntity user, bool silent = false)
        {
            return true;
        }

        private void ToggleOpen(IEntity user)
        {
            if (Open)
            {
                TryCloseStorage(user);
            }
            else
            {
                TryOpenStorage(user);
            }
        }

        protected virtual void CloseStorage()
        {
            Open = false;
            EntityQuery ??= new IntersectingEntityQuery(Owner);
            var entities = Owner.EntityManager.GetEntities(EntityQuery);
            var count = 0;
            foreach (var entity in entities)
            {
                // prevents taking items out of inventories, out of containers, and orphaning child entities
                if(!entity.Transform.IsMapTransform)
                    continue;

                // only items that can be stored in an inventory, or a mob, can be eaten by a locker
                if (!entity.HasComponent<StorableComponent>() &&
                    !entity.HasComponent<IBody>())
                    continue;

                if (!AddToContents(entity))
                {
                    continue;
                }
                count++;
                if (count >= _storageCapacityMax)
                {
                    break;
                }
            }

            ModifyComponents();
            EntitySystem.Get<AudioSystem>().PlayFromEntity(_closeSound, Owner);
            _lastInternalOpenAttempt = default;
        }

        protected virtual void OpenStorage()
        {
            Open = true;
            EmptyContents();
            ModifyComponents();
            EntitySystem.Get<AudioSystem>().PlayFromEntity(_openSound, Owner);
        }

        private void ModifyComponents()
        {
            if (!_isCollidableWhenOpen && Owner.TryGetComponent<IPhysicsComponent>(out var physics))
            {
                if (Open)
                {
                    physics.Hard = false;
                }
                else
                {
                    physics.Hard = true;
                }
            }

            if (Owner.TryGetComponent<PlaceableSurfaceComponent>(out var placeableSurfaceComponent))
            {
                placeableSurfaceComponent.IsPlaceable = Open;
            }

            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(StorageVisuals.Open, Open);
            }
        }

        protected virtual bool AddToContents(IEntity entity)
        {
            if (entity == Owner) return false;
            if (entity.TryGetComponent(out IPhysicsComponent? entityPhysicsComponent))
            {
                if(MaxSize < entityPhysicsComponent.WorldAABB.Size.X
                    || MaxSize < entityPhysicsComponent.WorldAABB.Size.Y)
                {
                    return false;
                }
            }
            if (Contents.CanInsert(entity))
            {
                Contents.Insert(entity);
                entity.Transform.LocalPosition = Vector2.Zero;
                if (entityPhysicsComponent != null)
                {
                    entityPhysicsComponent.CanCollide = false;
                }
                return true;
            }
            return false;
        }

        public virtual Vector2 ContentsDumpPosition()
        {
            return Owner.Transform.WorldPosition;
        }

        private void EmptyContents()
        {
            foreach (var contained in Contents.ContainedEntities.ToArray())
            {
                if(Contents.Remove(contained))
                {
                    contained.Transform.WorldPosition = ContentsDumpPosition();
                    if (contained.TryGetComponent<IPhysicsComponent>(out var physics))
                    {
                        physics.CanCollide = true;
                    }
                }
            }
        }

        /// <inheritdoc />
        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case RelayMovementEntityMessage msg:
                    if (msg.Entity.HasComponent<HandsComponent>())
                    {
                        if (_gameTiming.CurTime <
                            _lastInternalOpenAttempt + InternalOpenAttemptDelay)
                        {
                            break;
                        }

                        _lastInternalOpenAttempt = _gameTiming.CurTime;
                        TryOpenStorage(msg.Entity);
                    }
                    break;
            }
        }

        public virtual bool TryOpenStorage(IEntity user)
        {
            if (!CanOpen(user)) return false;
            OpenStorage();
            return true;
        }

        public virtual bool TryCloseStorage(IEntity user)
        {
            if (!CanClose(user)) return false;
            CloseStorage();
            return true;
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

            if (Contents.ContainedEntities.Count >= _storageCapacityMax)
            {
                return false;
            }

            return Contents.CanInsert(entity);
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (_beingWelded)
                return false;

            if (Open)
            {
                _beingWelded = false;
                return false;
            }

            if (!CanWeldShut)
            {
                _beingWelded = false;
                return false;
            }

            if (Contents.Contains(eventArgs.User))
            {
                _beingWelded = false;
                Owner.PopupMessage(eventArgs.User, Loc.GetString("It's too Cramped!"));
                return false;
            }

            if (!eventArgs.Using.TryGetComponent(out WelderComponent? tool) || !tool.WelderLit)
            {
                _beingWelded = false;
                return false;
            }

            if (_beingWelded)
                return false;

            _beingWelded = true;

            if (!await tool.UseTool(eventArgs.User, Owner, 1f, ToolQuality.Welding, 1f))
            {
                _beingWelded = false;
                return false;
            }

            _beingWelded = false;
            IsWeldedShut ^= true;
            return true;
        }

        void IDestroyAct.OnDestroy(DestructionEventArgs eventArgs)
        {
            Open = true;
            EmptyContents();
        }

        [Verb]
        private sealed class OpenToggleVerb : Verb<EntityStorageComponent>
        {
            protected override void GetData(IEntity user, EntityStorageComponent component, VerbData data)
            {
                if (!ActionBlockerSystem.CanInteract(user))
                {
                    data.Visibility = VerbVisibility.Invisible;
                    return;
                }

                component.OpenVerbGetData(user, component, data);
            }

            /// <inheritdoc />
            protected override void Activate(IEntity user, EntityStorageComponent component)
            {
                component.ToggleOpen(user);
            }
        }

        protected virtual void OpenVerbGetData(IEntity user, EntityStorageComponent component, VerbData data)
        {
            if (!ActionBlockerSystem.CanInteract(user))
            {
                data.Visibility = VerbVisibility.Invisible;
                return;
            }

            if (IsWeldedShut)
            {
                data.Visibility = VerbVisibility.Disabled;
                var verb = Loc.GetString(component.Open ? "Close" : "Open");
                data.Text = Loc.GetString("{0} (welded shut)", verb);
                return;
            }

            data.Text = Loc.GetString(component.Open ? "Close" : "Open");
        }

        void IExAct.OnExplosion(ExplosionEventArgs eventArgs)
        {
            if (eventArgs.Severity < ExplosionSeverity.Heavy)
            {
                return;
            }

            foreach (var entity in Contents.ContainedEntities)
            {
                var exActs = entity.GetAllComponents<IExAct>().ToArray();
                foreach (var exAct in exActs)
                {
                    exAct.OnExplosion(eventArgs);
                }
            }
        }
    }
}
