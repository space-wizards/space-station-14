using Content.Server.Placeable;
using Content.Server.Storage.Components;
using Content.Server.Tools.Components;
using Content.Shared.Acts;
using Content.Shared.Body.Components;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Notification.Managers;
using Content.Shared.Storage;
using Content.Shared.Tool;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using System.Collections.Generic;
using System.Linq;

namespace Content.Server.Storage.EntitySystems
{
    [UsedImplicitly]
    public class EntityStorageSystem : EntitySystem
    {
        private readonly List<IPlayerSession> _sessionCache = new();

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EntityStorageECSComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<EntityStorageECSComponent, ActivateInWorldEvent>(OnActivated);
            SubscribeLocalEvent<EntityStorageECSComponent, InteractUsingEvent>(OnInteracted);
            SubscribeLocalEvent<EntityStorageECSComponent, DestructionEventArgs>(OnDestroyed);
            //SubscribeLocalEvent<EntityStorageECSComponent, ExplosionEventArgs>(OnExplosion);
            //SubscribeLocalEvent<EntInsertedIntoContainerMessage>(HandleEntityInsertedIntoContainer);
        }

        private void OnInit(EntityUid eUI, EntityStorageECSComponent comp, ComponentInit args)
        {
            comp.Contents = comp.Owner.EnsureContainer<Container>(nameof(EntityStorageComponent));
            comp.Contents.ShowContents = comp.ShowContents;
            comp.Contents.OccludesLight = comp.OccludesLight;

            if (comp.Owner.TryGetComponent<PlaceableSurfaceComponent>(out var placeableSurfaceComponent))
            {
                placeableSurfaceComponent.IsPlaceable = comp.Open;
            }

            UpdateAppearance(comp);
        }

        private void OnActivated(EntityUid eUI, EntityStorageECSComponent comp, ActivateInWorldEvent args)
        {
            // HACK until EntityStorageComponent gets refactored to the new ECS system
            if (comp.Owner.TryGetComponent<LockComponent>(out var @lock) && @lock.Locked)
            {
                // Do nothing, LockSystem is responsible for handling this case
                return;
            }

            ToggleOpen(comp, args.User);
        }

        public virtual bool CanOpen(EntityStorageECSComponent comp, IEntity user, bool silent = false)
        {
            if (comp.IsWeldedShut)
            {
                if (!silent) comp.Owner.PopupMessage(user, Loc.GetString("entity-storage-component-welded-shut-message"));
                return false;
            }
            return true;
        }

        public virtual bool CanClose(IEntity user, bool silent = false)
        {
            return true;
        }

        private void ToggleOpen(EntityStorageECSComponent comp, IEntity user)
        {
            if (comp.Open)
            {
                TryCloseStorage(comp, user);
            }
            else
            {
                TryOpenStorage(comp, user);
            }
        }

        protected virtual void CloseStorage(EntityStorageECSComponent comp)
        {
            comp.Open = false;

            var count = 0;
            foreach (var entity in DetermineCollidingEntities(comp))
            {
                // prevents taking items out of inventories, out of containers, and orphaning child entities
                if (entity.IsInContainer())
                    continue;

                // only items that can be stored in an inventory, or a mob, can be eaten by a locker
                if (!entity.HasComponent<SharedItemComponent>() &&
                    !entity.HasComponent<SharedBodyComponent>())
                    continue;

                if (!AddToContents(comp, entity))
                {
                    continue;
                }
                count++;
                if (count >= comp.StorageCapacityMax)
                {
                    break;
                }
            }

            ModifyComponents(comp);
            SoundSystem.Play(Filter.Pvs(comp.Owner), comp.CloseSound.GetSound(), comp.Owner);
            comp.LastInternalOpenAttempt = default;
        }

        protected virtual void OpenStorage(EntityStorageECSComponent comp)
        {
            comp.Open = true;
            EmptyContents(comp);
            ModifyComponents(comp);
            SoundSystem.Play(Filter.Pvs(comp.Owner), comp.OpenSound.GetSound(), comp.Owner);
        }

        private void UpdateAppearance(EntityStorageECSComponent comp)
        {
            if (comp.Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(StorageVisuals.CanWeld, comp.CanWeldShut);
                appearance.SetData(StorageVisuals.Welded, comp.IsWeldedShut);
            }
        }

        private void ModifyComponents(EntityStorageECSComponent comp)
        {
            if (!comp.IsCollidableWhenOpen && comp.Owner.TryGetComponent<IPhysBody>(out var physics))
            {
                if (comp.Open)
                {
                    foreach (var fixture in physics.Fixtures)
                    {
                        fixture.CollisionLayer &= ~EntityStorageECSComponent.OpenMask;
                    }
                }
                else
                {
                    foreach (var fixture in physics.Fixtures)
                    {
                        fixture.CollisionLayer |= EntityStorageECSComponent.OpenMask;
                    }
                }
            }

            if (comp.Owner.TryGetComponent<PlaceableSurfaceComponent>(out var placeableSurfaceComponent))
            {
                placeableSurfaceComponent.IsPlaceable = comp.Open;
            }

            if (comp.Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(StorageVisuals.Open, comp.Open);
            }
        }

        protected virtual bool AddToContents(EntityStorageECSComponent comp, IEntity entity)
        {
            if (entity == comp.Owner) return false;
            if (entity.TryGetComponent(out IPhysBody? entityPhysicsComponent))
            {
                if (EntityStorageECSComponent.MaxSize < entityPhysicsComponent.GetWorldAABB().Size.X
                    || EntityStorageECSComponent.MaxSize < entityPhysicsComponent.GetWorldAABB().Size.Y)
                {
                    return false;
                }
            }

            return comp.Contents.CanInsert(entity) && Insert(comp, entity);
        }

        public virtual Vector2 ContentsDumpPosition(EntityStorageECSComponent comp)
        {
            return comp.Owner.Transform.WorldPosition;
        }

        private void EmptyContents(EntityStorageECSComponent comp)
        {
            foreach (var contained in comp.Contents.ContainedEntities.ToArray())
            {
                if (comp.Contents.Remove(contained))
                {
                    contained.Transform.WorldPosition = ContentsDumpPosition(comp);
                    if (contained.TryGetComponent<IPhysBody>(out var physics))
                    {
                        physics.CanCollide = true;
                    }
                }
            }
        }

        ///// <inheritdoc />
        //public override void HandleMessage(ComponentMessage message, IComponent? component)
        //{
        //    base.HandleMessage(message, component);

        //    switch (message)
        //    {
        //        case RelayMovementEntityMessage msg:
        //            if (msg.Entity.HasComponent<HandsComponent>())
        //            {
        //                if (_gameTiming.CurTime <
        //                    _lastInternalOpenAttempt + InternalOpenAttemptDelay)
        //                {
        //                    break;
        //                }

        //                _lastInternalOpenAttempt = _gameTiming.CurTime;
        //                TryOpenStorage(msg.Entity);
        //            }
        //            break;
        //    }
        //}

        public virtual bool TryOpenStorage(EntityStorageECSComponent comp, IEntity user)
        {
            if (!CanOpen(comp, user)) return false;
            OpenStorage(comp);
            return true;
        }

        public virtual bool TryCloseStorage(EntityStorageECSComponent comp, IEntity user)
        {
            if (!CanClose(user)) return false;
            CloseStorage(comp);
            return true;
        }

        /// <inheritdoc />
        public bool Remove(EntityStorageECSComponent comp, IEntity entity)
        {
            return comp.Contents.CanRemove(entity);
        }

        /// <inheritdoc />
        public bool Insert(EntityStorageECSComponent comp, IEntity entity)
        {
            // Trying to add while open just dumps it on the ground below us.
            if (comp.Open)
            {
                entity.Transform.WorldPosition = comp.Owner.Transform.WorldPosition;
                return true;
            }

            if (!comp.Contents.Insert(entity)) return false;

            entity.Transform.LocalPosition = Vector2.Zero;
            if (entity.TryGetComponent(out IPhysBody? body))
            {
                body.CanCollide = false;
            }
            return true;
        }

        /// <inheritdoc />
        public bool CanInsert(EntityStorageECSComponent comp, IEntity entity)
        {
            if (comp.Open)
            {
                return true;
            }

            if (comp.Contents.ContainedEntities.Count >= comp.StorageCapacityMax)
            {
                return false;
            }

            return comp.Contents.CanInsert(entity);
        }

        private void OnInteracted(EntityUid eUI, EntityStorageECSComponent comp, InteractUsingEvent eventArgs)
        {
            if (comp.BeingWelded)
                return;

            if (comp.Open)
            {
                comp.BeingWelded = false;
                return;
            }

            if (!comp.CanWeldShut)
            {
                comp.BeingWelded = false;
                return;
            }

            if (comp.Contents.Contains(eventArgs.User))
            {
                comp.BeingWelded = false;
                comp.Owner.PopupMessage(eventArgs.User, Loc.GetString("entity-storage-component-already-contains-user-message"));
                return;
            }

            if (!eventArgs.Used.TryGetComponent(out WelderComponent? tool) || !tool.WelderLit)
            {
                comp.BeingWelded = false;
                return;
            }

            if (comp.BeingWelded)
                return;

            comp.BeingWelded = true;

            if (!tool.UseTool(eventArgs.User, comp.Owner, 1f, ToolQuality.Welding, 1f).GetAwaiter().GetResult())
            {
                comp.BeingWelded = false;
                return;
            }

            comp.BeingWelded = false;
            comp.IsWeldedShut ^= true;
        }

        private void OnDestroyed(EntityUid eUI, EntityStorageECSComponent comp, DestructionEventArgs eventArgs)
        {
            comp.Open = true;
            EmptyContents(comp);
        }

        protected virtual IEnumerable<IEntity> DetermineCollidingEntities(EntityStorageECSComponent comp)
        {
            var entityLookup = IoCManager.Resolve<IEntityLookup>();
            return entityLookup.GetEntitiesIntersecting(comp.Owner);
        }

        private void OnExplosion(EntityUid eUI, EntityStorageECSComponent comp, ExplosionEventArgs eventArgs)
        {
            if (eventArgs.Severity < ExplosionSeverity.Heavy)
            {
                return;
            }

            var containedEntities = comp.Contents.ContainedEntities.ToList();
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
