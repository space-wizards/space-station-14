#nullable enable
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
    public class SharedEntityInputComponent : Component, ICollideSpecial
    {
        public override string Name => "EntityInput";

        private readonly List<IEntity> _intersecting = new();

        /// <summary>
        /// The time between updating the list of intersecting entities.
        /// </summary>
        private const int UpdateDelay = 20;

        private CancellationTokenSource? _updateToken;

        /// <summary>
        /// Prevents entities from colliding until the exit the collider
        /// </summary>
        bool ICollideSpecial.PreventCollide(IPhysBody collided)
        {
            if (IsExiting(collided.Entity)) return true;
            if (!Owner.TryGetComponent(out IContainerManager? manager)) return false;

            if (manager.ContainsEntity(collided.Entity))
            {
                if (!_intersecting.Contains(collided.Entity))
                {
                    _intersecting.Add(collided.Entity);
                    _updateToken?.Cancel();
                    _updateToken = null;

                    StartUpdateTimer();
                }
                return true;
            }
            return false;
        }

        private bool IsExiting(IEntity entity)
        {
            return _intersecting.Contains(entity);
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

            if(_intersecting.Count != 0)
            {
                StartUpdateTimer();
            }
        }

        private void StartUpdateTimer()
        {
            _updateToken = new CancellationTokenSource();
            Owner.SpawnTimer(UpdateDelay, UpdateIntersecting, _updateToken.Token);
        }
    }
}
