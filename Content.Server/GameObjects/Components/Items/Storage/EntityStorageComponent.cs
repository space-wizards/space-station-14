#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.GameObjects.Components.GUI;
using Content.Server.GameObjects.Components.Interactable;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Interactable;
using Content.Shared.GameObjects.Components.Storage;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
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

        private const int OpenMask = (int) (
            CollisionGroup.MobImpassable |
            CollisionGroup.VaultImpassable |
            CollisionGroup.SmallImpassable);

        [ViewVariables]
        [DataField("Capacity")]
        private int _storageCapacityMax = 30;

        [ViewVariables]
        [DataField("IsCollidableWhenOpen")]
        private bool _isCollidableWhenOpen;

        [ViewVariables]
        protected IEntityQuery? EntityQuery;

        [DataField("showContents")]
        private bool _showContents;

        [DataField("occludesLight")]
        private bool _occludesLight = true;

        [DataField("open")]
        private bool _open;

        [DataField("CanWeldShut")]
        private bool _canWeldShut = true;

        [DataField("IsWeldedShut")]
        private bool _isWeldedShut;

        [DataField("closeSound")]
        private string _closeSound = "/Audio/Machines/closetclose.ogg";

        [DataField("openSound")]
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
                if (_isWeldedShut == value) return;

                _isWeldedShut = value;
                UpdateAppearance();
            }
        }

        private bool _beingWelded;

        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanWeldShut {
            get => _canWeldShut;
            set
            {
                if (_canWeldShut == value) return;

                _canWeldShut = value;
                UpdateAppearance();
            }
        }

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();
            Contents = Owner.EnsureContainer<Container>(nameof(EntityStorageComponent));
            EntityQuery = new IntersectingEntityQuery(Owner);

            Contents.ShowContents = _showContents;
            Contents.OccludesLight = _occludesLight;

            if (Owner.TryGetComponent<PlaceableSurfaceComponent>(out var placeableSurfaceComponent))
            {
                placeableSurfaceComponent.IsPlaceable = Open;
            }

            UpdateAppearance();
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
                if (!entity.HasComponent<SharedItemComponent>() &&
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
            SoundSystem.Play(Filter.Pvs(Owner), _closeSound, Owner);
            _lastInternalOpenAttempt = default;
        }

        protected virtual void OpenStorage()
        {
            Open = true;
            EmptyContents();
            ModifyComponents();
            SoundSystem.Play(Filter.Pvs(Owner), _openSound, Owner);
        }

        private void UpdateAppearance()
        {
            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(StorageVisuals.CanWeld, _canWeldShut);
                appearance.SetData(StorageVisuals.Welded, _isWeldedShut);
            }
        }

        private void ModifyComponents()
        {
            if (!_isCollidableWhenOpen && Owner.TryGetComponent<IPhysBody>(out var physics))
            {
                if (Open)
                {
                    foreach (var fixture in physics.Fixtures)
                    {
                        fixture.CollisionLayer &= ~OpenMask;
                    }
                }
                else
                {
                    foreach (var fixture in physics.Fixtures)
                    {
                        fixture.CollisionLayer |= OpenMask;
                    }
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
            if (entity.TryGetComponent(out IPhysBody? entityPhysicsComponent))
            {
                if (MaxSize < entityPhysicsComponent.GetWorldAABB().Size.X
                    || MaxSize < entityPhysicsComponent.GetWorldAABB().Size.Y)
                {
                    return false;
                }
            }

            return Contents.CanInsert(entity) && Insert(entity);
        }

        public virtual Vector2 ContentsDumpPosition()
        {
            return Owner.Transform.WorldPosition;
        }

        private void EmptyContents()
        {
            foreach (var contained in Contents.ContainedEntities.ToArray())
            {
                if (Contents.Remove(contained))
                {
                    contained.Transform.WorldPosition = ContentsDumpPosition();
                    if (contained.TryGetComponent<IPhysBody>(out var physics))
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

            if (!Contents.Insert(entity)) return false;

            entity.Transform.LocalPosition = Vector2.Zero;
            if (entity.TryGetComponent(out IPhysBody? body))
            {
                body.CanCollide = false;
            }
            return true;
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
            data.IconTexture = component.Open ? "/Textures/Interface/VerbIcons/close.svg.192dpi.png" : "/Textures/Interface/VerbIcons/open.svg.192dpi.png";
        }

        void IExAct.OnExplosion(ExplosionEventArgs eventArgs)
        {
            if (eventArgs.Severity < ExplosionSeverity.Heavy)
            {
                return;
            }

            var containedEntities = Contents.ContainedEntities.ToList();
            foreach (var entity in containedEntities)
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
