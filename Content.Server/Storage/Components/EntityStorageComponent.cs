using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Tools.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Acts;
using Content.Shared.Body.Components;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Physics;
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Content.Shared.Storage;
using Content.Shared.Tool;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Storage.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IStorageComponent))]
    public class EntityStorageComponent : Component, IActivate, IStorageComponent, IInteractUsing, IDestroyAct, IActionBlocker, IExAct
    {
        public override string Name => "EntityStorage";

        private const float MaxSize = 1.0f; // maximum width or height of an entity allowed inside the storage.

        public static readonly TimeSpan InternalOpenAttemptDelay = TimeSpan.FromSeconds(0.5);
        public TimeSpan LastInternalOpenAttempt;

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
        private SoundSpecifier _closeSound = new SoundPathSpecifier("/Audio/Effects/closetclose.ogg");

        [DataField("openSound")]
        private SoundSpecifier _openSound = new SoundPathSpecifier("/Audio/Effects/closetopen.ogg");

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
        public bool CanWeldShut
        {
            get => _canWeldShut;
            set
            {
                if (_canWeldShut == value) return;

                _canWeldShut = value;
                UpdateAppearance();
            }
        }

        /// <inheritdoc />
        protected override void Initialize()
        {
            base.Initialize();
            Contents = Owner.EnsureContainer<Container>(nameof(EntityStorageComponent));
            Contents.ShowContents = _showContents;
            Contents.OccludesLight = _occludesLight;

            if (Owner.TryGetComponent<PlaceableSurfaceComponent>(out var surface))
            {
                EntitySystem.Get<PlaceableSurfaceSystem>().SetPlaceable(surface, Open);
            }

            UpdateAppearance();
        }

        public virtual void Activate(ActivateEventArgs eventArgs)
        {
            // HACK until EntityStorageComponent gets refactored to the new ECS system
            if (Owner.TryGetComponent<LockComponent>(out var @lock) && @lock.Locked)
            {
                // Do nothing, LockSystem is responsible for handling this case
                return;
            }

            ToggleOpen(eventArgs.User);
        }

        public virtual bool CanOpen(IEntity user, bool silent = false)
        {
            if (IsWeldedShut)
            {
                if (!silent) Owner.PopupMessage(user, Loc.GetString("entity-storage-component-welded-shut-message"));
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

            var count = 0;
            foreach (var entity in DetermineCollidingEntities())
            {
                // prevents taking items out of inventories, out of containers, and orphaning child entities
                if (entity.IsInContainer())
                    continue;

                // only items that can be stored in an inventory, or a mob, can be eaten by a locker
                if (!entity.HasComponent<SharedItemComponent>() &&
                    !entity.HasComponent<SharedBodyComponent>())
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
                SoundSystem.Play(Filter.Pvs(Owner), _closeSound.GetSound(), Owner);
            LastInternalOpenAttempt = default;
        }

        protected virtual void OpenStorage()
        {
            Open = true;
            EmptyContents();
            ModifyComponents();
                SoundSystem.Play(Filter.Pvs(Owner), _openSound.GetSound(), Owner);
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

            if (Owner.TryGetComponent<PlaceableSurfaceComponent>(out var surface))
            {
                EntitySystem.Get<PlaceableSurfaceSystem>().SetPlaceable(surface, Open);
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
                Owner.PopupMessage(eventArgs.User, Loc.GetString("entity-storage-component-already-contains-user-message"));
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

        protected virtual IEnumerable<IEntity> DetermineCollidingEntities()
        {
            var entityLookup = IoCManager.Resolve<IEntityLookup>();
            return entityLookup.GetEntitiesIntersecting(Owner);
        }

        [Verb]
        private sealed class OpenToggleVerb : Verb<EntityStorageComponent>
        {
            protected override void GetData(IEntity user, EntityStorageComponent component, VerbData data)
            {
                if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user))
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
            if (!EntitySystem.Get<ActionBlockerSystem>().CanInteract(user) ||
                component.Owner.TryGetComponent(out LockComponent? lockComponent) && lockComponent.Locked) // HACK extra check, until EntityStorage gets refactored
            {
                data.Visibility = VerbVisibility.Invisible;
                return;
            }

            if (IsWeldedShut)
            {
                data.Visibility = VerbVisibility.Disabled;
                var verb = Loc.GetString(component.Open ? "open-toggle-verb-close" : "open-toggle-verb-open");
                data.Text = Loc.GetString("open-toggle-verb-welded-shut-message", ("verb", verb));
                return;
            }

            data.Text = Loc.GetString(component.Open ? "open-toggle-verb-close" : "open-toggle-verb-open");
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
