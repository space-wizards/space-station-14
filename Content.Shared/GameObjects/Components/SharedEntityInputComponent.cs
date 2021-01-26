#nullable enable
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Items;
using Content.Shared.GameObjects.Components.Mobs.State;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Timers;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Physics;
using System.Collections.Generic;
using System.Threading;

namespace Content.Shared.GameObjects.Components
{
    public abstract class SharedEntityInputComponent : Component, ICollideSpecial, IDragDropOn
    {
        public override string Name => "EntityInput";

        private readonly List<IEntity> _intersecting = new();

        public virtual bool CanInsert(IEntity entity)
        {
            if (!entity.TryGetComponent(out IPhysicsComponent? physics) || !physics.CanCollide)
            {
                if (entity.TryGetComponent(out IMobStateComponent? state) && state.IsDead())
                {
                    return false;
                }
            }

            if (!entity.HasComponent<IItemComponent>() && !entity.HasComponent<IBody>())
            {
                return false;
            }

            return true;
        }

        public virtual bool CanDragDropOn(DragDropEventArgs eventArgs)
        {
            return CanInsert(eventArgs.Dragged);
        }

        public abstract bool DragDropOn(DragDropEventArgs eventArgs);

        public void Update()
        {
            UpdateIntersecting();
        }

        /// <summary>
        /// Prevents entities from colliding until the exit the collider
        /// </summary>
        bool ICollideSpecial.PreventCollide(IPhysBody collided)
        {
            var entity = collided.Entity;
            if (!Owner.TryGetComponent(out IContainerManager? manager)) return false;
            if (_intersecting.Contains(entity)) return true;

            if (manager.ContainsEntity(entity))
            {
                if (!_intersecting.Contains(entity))
                {
                    _intersecting.Add(entity);
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the entities in _intersecting are still intersecting the collider
        /// and removes them from the list if they aren`t.
        /// </summary>
        private void UpdateIntersecting()
        {
            if (_intersecting.Count == 0) return;

            for (var i = _intersecting.Count - 1; i >= 0; i--)
            {
                var entity = _intersecting[i];

                if (!Owner.EntityManager.IsIntersecting(entity, Owner))
                    _intersecting.RemoveAt(i);
            }
        }
    }
}
