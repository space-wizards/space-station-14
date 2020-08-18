using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.GameObjects.Components;
using Content.Shared.Physics;
using Content.Shared.Maps;
using Robust.Shared.IoC;
using Robust.Shared.Interfaces.GameObjects;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.EntitySystems;
using System.Collections.Generic;

namespace Content.Server.GameObjects.Components.Movement
{
    [RegisterComponent]
    public class ClimbingComponent : SharedClimbingComponent, IActionBlocker
    {
        public override void Initialize()
        {
            base.Initialize();

            Owner.TryGetComponent(out _body);
        }

        private ICollidableComponent _body = default;
        private bool _isClimbing = false;
        private ClimbController _climbController = default;

        public bool IsClimbing
        {
            get
            {
                return _isClimbing;
            }
            set
            {
                if (!value && _body != null)
                {
                    _body.TryRemoveController<ClimbController>();
                }

                _isClimbing = value;
                UpdateCollision();
                Dirty();
            }
        }

        private bool OwnerIsTransitioning
        {
            get
            {
                if (_climbController != null)
                {
                    return !_climbController.IsActive;
                }

                return true;
            }
        }

        bool IActionBlocker.CanMove() => OwnerIsTransitioning;
        bool IActionBlocker.CanChangeDirection() => OwnerIsTransitioning;

        public void TryMoveTo(Vector2 from, Vector2 to)
        {
            if (_body != null)
            {
                _climbController = _body.EnsureController<ClimbController>();
                _climbController.TryMoveTo(from, to);
            }
        }

        public void Update(float frameTime) 
        {
            if (_body != null && _isClimbing)
            {
                if (_climbController != null && _climbController.IsBlocked)
                {
                    _body.TryRemoveController<ClimbController>();
                }

                var tile = TurfHelpers.GetTileRef(Owner.Transform.GridPosition);

                if (tile.HasValue) // GetEntitiesIntersectingEntity would have been nicer but it's expensive.
                {
                    foreach (var entity in TurfHelpers.GetEntitiesInTile(tile.Value))
                    {
                        if (entity.HasComponent<ClimbableComponent>())
                        {
                            return;
                        }
                    }
                }

                IsClimbing = false; // there are no climbables within the tile we stand on
            }
        }

        // Helper that returns all entities intersecting this entity
        // This is too expensive to use every update :(
        public IEnumerable<IEntity> GetEntitiesIntersectingEntity(bool approximate = false)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (Owner.TryGetComponent(out ICollidableComponent body))
            {
                return entityManager.GetEntitiesIntersecting(Owner.Transform.MapID, body.WorldAABB, approximate);
            }

            return new IEntity[0];
        }

        private void UpdateCollision()
        {
            if (_body == null)
            {
                return;
            }

            if (_isClimbing)
            {
                _body.PhysicsShapes[0].CollisionMask &= ~((int) CollisionGroup.VaultImpassable);
            }
            else
            {
                _body.PhysicsShapes[0].CollisionMask |= ((int) CollisionGroup.VaultImpassable);
            }
        }

        public override ComponentState GetComponentState()
        {
            return new ClimbModeComponentState(_isClimbing);
        }
    }
}
