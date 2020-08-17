using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.GameObjects.Components;
using Content.Shared.Physics;
using Content.Shared.Maps;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.EntitySystems;

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

                // We should be using AABB checks to unclimb but i can't think of a cheap way to do it so for now let's just check if the user's grid position has climbables

                var tile = TurfHelpers.GetTileRef(Owner.Transform.GridPosition);

                if (tile.HasValue)
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
