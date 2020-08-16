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
    public class ClimbModeComponent : SharedClimbModeComponent, IActionBlocker
    {
        public override void Initialize()
        {
            base.Initialize();

            Owner.TryGetComponent(out ICollidableComponent body);
            _body = body;
        }

        private ICollidableComponent _body = default;
        private bool _isClimbing = false;
        private ClimbController _climbController = default;

        bool IActionBlocker.CanMove() => OwnerIsTransitioning();
        bool IActionBlocker.CanChangeDirection() => OwnerIsTransitioning();

        private bool OwnerIsTransitioning()
        {
            if (_climbController != null)
            {
                return !_climbController.IsActive;
            }

            return true;
        }

        public void SetClimbing(bool isClimbing)
        {
            if (!isClimbing && _body != null)
            {
                _body.TryRemoveController<ClimbController>();
            }

            _isClimbing = isClimbing;
            UpdateCollision();
            Dirty();
        }

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

                var tile = (TileRef)TurfHelpers.GetTileRef(Owner.Transform.GridPosition);

                foreach (var entity in TurfHelpers.GetEntitiesInTile(tile))
                {
                    if (entity.HasComponent<ClimbableComponent>())
                    {
                        return;
                    }
                }

                SetClimbing(false); // there are no climbables within the tile we stand on
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
