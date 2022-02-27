using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Server.Ghost.Components;
using Content.Server.Tools;
using Content.Shared.Acts;
using Content.Shared.Body.Components;
using Content.Shared.Foldable;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Physics;
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Content.Shared.Storage;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.Storage.Components
{
    [RegisterComponent]
    [Virtual]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IStorageComponent))]
    public class EntityStorageComponent : Component, IActivate, IStorageComponent, IInteractUsing, IDestroyAct
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

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

        [ViewVariables]
        [DataField("EnteringRange")]
        private float _enteringRange = -0.4f;

        [DataField("showContents")]
        private bool _showContents;

        [DataField("occludesLight")]
        private bool _occludesLight = true;

        [DataField("open")]
        private bool _open;

        [DataField("weldingQuality", customTypeSerializer:typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
        private string _weldingQuality = "Welding";

        [DataField("CanWeldShut")]
        private bool _canWeldShut = true;

        [DataField("IsWeldedShut")]
        private bool _isWeldedShut;

        [DataField("closeSound")]
        private SoundSpecifier _closeSound = new SoundPathSpecifier("/Audio/Effects/closetclose.ogg");

        [DataField("openSound")]
        private SoundSpecifier _openSound = new SoundPathSpecifier("/Audio/Effects/closetopen.ogg");

        [ViewVariables]
        public Container Contents = default!;

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

        [ViewVariables(VVAccess.ReadWrite)]
        public float EnteringRange
        {
            get => _enteringRange;
            set => _enteringRange = value;
        }

        /// <inheritdoc />
        protected override void Initialize()
        {
            base.Initialize();
            Contents = Owner.EnsureContainer<Container>(nameof(EntityStorageComponent));
            Contents.ShowContents = _showContents;
            Contents.OccludesLight = _occludesLight;

            if(_entMan.TryGetComponent(Owner, out ConstructionComponent? construction))
                EntitySystem.Get<ConstructionSystem>().AddContainer(Owner, Contents.ID, construction);

            if (_entMan.TryGetComponent<PlaceableSurfaceComponent?>(Owner, out var surface))
            {
                EntitySystem.Get<PlaceableSurfaceSystem>().SetPlaceable(Owner, Open, surface);
            }

            UpdateAppearance();
        }

        public virtual void Activate(ActivateEventArgs eventArgs)
        {
            ToggleOpen(eventArgs.User);
        }

        public virtual bool CanOpen(EntityUid user, bool silent = false)
        {
            if (IsWeldedShut)
            {
                if (!silent && !Contents.Contains(user))
                    Owner.PopupMessage(user, Loc.GetString("entity-storage-component-welded-shut-message"));

                return false;
            }

            if (_entMan.TryGetComponent<LockComponent?>(Owner, out var @lock) && @lock.Locked)
            {
                if (!silent) Owner.PopupMessage(user, Loc.GetString("entity-storage-component-locked-message"));
                return false;
            }

            var @event = new StorageOpenAttemptEvent();
            IoCManager.Resolve<IEntityManager>().EventBus.RaiseLocalEvent(Owner, @event);

            return !@event.Cancelled;
        }

        public virtual bool CanClose(EntityUid user, bool silent = false)
        {
            var @event = new StorageCloseAttemptEvent();
            IoCManager.Resolve<IEntityManager>().EventBus.RaiseLocalEvent(Owner, @event);

            return !@event.Cancelled;
        }

        public void ToggleOpen(EntityUid user)
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

                // conditions are complicated because of pizzabox-related issues, so follow this guide
                // 0. Accomplish your goals at all costs.
                // 1. AddToContents can block anything
                // 2. maximum item count can block anything
                // 3. ghosts can NEVER be eaten
                // 4. items can always be eaten unless a previous law prevents it
                // 5. if this is NOT AN ITEM, then mobs can always be eaten unless unless a previous law prevents it
                // 6. if this is an item, then mobs must only be eaten if some other component prevents pick-up interactions while a mob is inside (e.g. foldable)

                // Let's not insert admin ghosts, yeah? This is really a a hack and should be replaced by attempt events
                if (_entMan.HasComponent<GhostComponent>(entity))
                    continue;

                // checks

                var targetIsItem = _entMan.HasComponent<SharedItemComponent>(entity);
                var targetIsMob = _entMan.HasComponent<SharedBodyComponent>(entity);
                var storageIsItem = _entMan.HasComponent<SharedItemComponent>(Owner);

                var allowedToEat = false;

                if (targetIsItem)
                    allowedToEat = true;

                // BEFORE REPLACING THIS WITH, I.E. A PROPERTY:
                // Make absolutely 100% sure you have worked out how to stop people ending up in backpacks.
                // Seriously, it is insanely hacky and weird to get someone out of a backpack once they end up in there.
                // And to be clear, they should NOT be in there.
                // For the record, what you need to do is empty the backpack onto a PlacableSurface (table, rack)
                if (targetIsMob)
                {
                    if (!storageIsItem)
                        allowedToEat = true;
                    else
                    {
                        // make an exception if this is a foldable-item that is currently un-folded (e.g., body bags).
                        allowedToEat = _entMan.TryGetComponent(Owner, out FoldableComponent? foldable) && !foldable.IsFolded;
                    }
                }

                if (!allowedToEat)
                    continue;

                // finally, AddToContents

                if (!AddToContents(entity))
                    continue;

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
            if (_entMan.TryGetComponent(Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(StorageVisuals.CanWeld, _canWeldShut);
                appearance.SetData(StorageVisuals.Welded, _isWeldedShut);
            }
        }

        private void ModifyComponents()
        {
            if (!_isCollidableWhenOpen && _entMan.TryGetComponent<FixturesComponent?>(Owner, out var manager))
            {
                if (Open)
                {
                    foreach (var (_, fixture) in manager.Fixtures)
                    {
                        fixture.CollisionLayer &= ~OpenMask;
                    }
                }
                else
                {
                    foreach (var (_, fixture) in manager.Fixtures)
                    {
                        fixture.CollisionLayer |= OpenMask;
                    }
                }
            }

            if (_entMan.TryGetComponent<PlaceableSurfaceComponent?>(Owner, out var surface))
            {
                EntitySystem.Get<PlaceableSurfaceSystem>().SetPlaceable(Owner, Open, surface);
            }

            if (_entMan.TryGetComponent(Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(StorageVisuals.Open, Open);
            }
        }

        protected virtual bool AddToContents(EntityUid entity)
        {
            if (entity == Owner) return false;
            if (_entMan.TryGetComponent(entity, out IPhysBody? entityPhysicsComponent))
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
            return _entMan.GetComponent<TransformComponent>(Owner).WorldPosition;
        }

        private void EmptyContents()
        {
            foreach (var contained in Contents.ContainedEntities.ToArray())
            {
                if (Contents.Remove(contained))
                {
                    _entMan.GetComponent<TransformComponent>(contained).WorldPosition = ContentsDumpPosition();
                    if (_entMan.TryGetComponent<IPhysBody?>(contained, out var physics))
                    {
                        physics.CanCollide = true;
                    }
                }
            }
        }

        public virtual bool TryOpenStorage(EntityUid user)
        {
            if (!CanOpen(user)) return false;
            OpenStorage();
            return true;
        }

        public virtual bool TryCloseStorage(EntityUid user)
        {
            if (!CanClose(user)) return false;
            CloseStorage();
            return true;
        }

        /// <inheritdoc />
        public bool Remove(EntityUid entity)
        {
            return Contents.CanRemove(entity);
        }

        /// <inheritdoc />
        public bool Insert(EntityUid entity)
        {
            // Trying to add while open just dumps it on the ground below us.
            if (Open)
            {
                var entMan = _entMan;
                entMan.GetComponent<TransformComponent>(entity).WorldPosition = entMan.GetComponent<TransformComponent>(Owner).WorldPosition;
                return true;
            }

            return Contents.Insert(entity);
        }

        /// <inheritdoc />
        public bool CanInsert(EntityUid entity)
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

            if (_beingWelded)
                return false;

            _beingWelded = true;

            var toolSystem = EntitySystem.Get<ToolSystem>();

            if (!await toolSystem.UseTool(eventArgs.Using, eventArgs.User, Owner, 1f, 1f, _weldingQuality))
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

        protected virtual IEnumerable<EntityUid> DetermineCollidingEntities()
        {
            var entityLookup = IoCManager.Resolve<IEntityLookup>();
            return entityLookup.GetEntitiesIntersecting(Owner, _enteringRange, LookupFlags.Approximate);
        }
    }

    public sealed class StorageOpenAttemptEvent : CancellableEntityEventArgs
    {

    }

    public sealed class StorageCloseAttemptEvent : CancellableEntityEventArgs
    {

    }
}
