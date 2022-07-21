using System.Linq;
using Content.Server.Buckle.Components;
using Content.Server.Construction;
using Content.Server.Construction.Completions;
using Content.Server.Construction.Components;
using Content.Server.Ghost.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Body.Components;
using Content.Shared.Foldable;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Physics;
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Content.Shared.Storage;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Player;

namespace Content.Server.Storage.Components
{
    [RegisterComponent]
    [Virtual]
    [ComponentReference(typeof(IActivate))]
    [ComponentReference(typeof(IStorageComponent))]
    public class EntityStorageComponent : Component, IActivate, IStorageComponent
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        private const float MaxSize = 1.0f; // maximum width or height of an entity allowed inside the storage.

        public static readonly TimeSpan InternalOpenAttemptDelay = TimeSpan.FromSeconds(0.5);
        public TimeSpan LastInternalOpenAttempt;

        /// <summary>
        ///     Collision masks that get removed when the storage gets opened.
        /// </summary>
        private const int MasksToRemove = (int) (
            CollisionGroup.MidImpassable |
            CollisionGroup.HighImpassable |
            CollisionGroup.LowImpassable);

        /// <summary>
        ///     Collision masks that were removed from ANY layer when the storage was opened;
        /// </summary>
        [DataField("removedMasks")] public int RemovedMasks;

        [ViewVariables]
        [DataField("Capacity")]
        private int _storageCapacityMax = 30;

        [ViewVariables]
        [DataField("IsCollidableWhenOpen")]
        private bool _isCollidableWhenOpen;

        [ViewVariables]
        [DataField("EnteringRange")]
        private float _enteringRange = -0.18f;

        [DataField("showContents")]
        private bool _showContents;

        [DataField("occludesLight")]
        private bool _occludesLight = true;

        [DataField("open")]
        public bool Open;

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
        public bool IsWeldedShut;

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
            Contents = Owner.EnsureContainer<Container>(EntityStorageSystem.ContainerName);
            Contents.ShowContents = _showContents;
            Contents.OccludesLight = _occludesLight;

            if(_entMan.TryGetComponent(Owner, out ConstructionComponent? construction))
                EntitySystem.Get<ConstructionSystem>().AddContainer(Owner, nameof(EntityStorageComponent), construction);

            if (_entMan.TryGetComponent<PlaceableSurfaceComponent?>(Owner, out var surface))
            {
                EntitySystem.Get<PlaceableSurfaceSystem>().SetPlaceable(Owner, Open, surface);
            }
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
            IoCManager.Resolve<IEntityManager>().EventBus.RaiseLocalEvent(Owner, @event, true);

            return !@event.Cancelled;
        }

        public virtual bool CanClose(EntityUid user, bool silent = false)
        {
            var @event = new StorageCloseAttemptEvent();
            IoCManager.Resolve<IEntityManager>().EventBus.RaiseLocalEvent(Owner, @event, true);

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

                if (!CanFit(entity))
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
                SoundSystem.Play(_closeSound.GetSound(), Filter.Pvs(Owner), Owner);
            LastInternalOpenAttempt = default;
        }

        public virtual bool CanFit(EntityUid entity)
        {
            // conditions are complicated because of pizzabox-related issues, so follow this guide
            // 0. Accomplish your goals at all costs.
            // 1. AddToContents can block anything
            // 2. maximum item count can block anything
            // 3. ghosts can NEVER be eaten
            // 4. items can always be eaten unless a previous law prevents it
            // 5. if this is NOT AN ITEM, then mobs can always be eaten unless unless a previous law prevents it
            // 6. if this is an item, then mobs must only be eaten if some other component prevents pick-up interactions while a mob is inside (e.g. foldable)
            var attemptEvent = new InsertIntoEntityStorageAttemptEvent();
            _entMan.EventBus.RaiseLocalEvent(entity, attemptEvent);
            if (attemptEvent.Cancelled)
                return false;

            // checks
            // TODO: Make the others sub to it.
            var targetIsItem = _entMan.HasComponent<SharedItemComponent>(entity);
            var targetIsMob = _entMan.HasComponent<SharedBodyComponent>(entity);
            var storageIsItem = _entMan.HasComponent<SharedItemComponent>(Owner);

            var allowedToEat = targetIsItem;

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
                    var storeEv = new StoreThisAttemptEvent();
                    _entMan.EventBus.RaiseLocalEvent(Owner, storeEv);
                    allowedToEat = !storeEv.Cancelled;
                }
            }

            return allowedToEat;
        }

        protected virtual void OpenStorage()
        {
            Open = true;
            EntitySystem.Get<EntityStorageSystem>().EmptyContents(Owner, this);
            ModifyComponents();
                SoundSystem.Play(_openSound.GetSound(), Filter.Pvs(Owner), Owner);
        }

        private void ModifyComponents()
        {
            if (!_isCollidableWhenOpen && _entMan.TryGetComponent<FixturesComponent?>(Owner, out var manager)
                && manager.Fixtures.Count > 0)
            {
                // currently only works for single-fixture entities. If they have more than one fixture, then
                // RemovedMasks needs to be tracked separately for each fixture, using a fixture Id Dictionary. Also the
                // fixture IDs probably cant be automatically generated without causing issues, unless there is some
                // guarantee that they will get deserialized with the same auto-generated ID when saving+loading the map.
                var fixture = manager.Fixtures.Values.First();

                if (Open)
                {
                    RemovedMasks = fixture.CollisionLayer & MasksToRemove;
                    fixture.CollisionLayer &= ~MasksToRemove;
                }
                else
                {
                    fixture.CollisionLayer |= RemovedMasks;
                    RemovedMasks = 0;
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

        protected virtual IEnumerable<EntityUid> DetermineCollidingEntities()
        {
            var entityLookup = EntitySystem.Get<EntityLookupSystem>();
            return entityLookup.GetEntitiesInRange(Owner, _enteringRange, LookupFlags.Approximate);
        }
    }

    public sealed class InsertIntoEntityStorageAttemptEvent : CancellableEntityEventArgs
    {

    }

    public sealed class StoreThisAttemptEvent : CancellableEntityEventArgs
    {

    }
    public sealed class StorageOpenAttemptEvent : CancellableEntityEventArgs
    {

    }

    public sealed class StorageCloseAttemptEvent : CancellableEntityEventArgs
    {

    }
}
