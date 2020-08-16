using Robust.Shared.GameObjects;
using Content.Server.Interfaces;
using Content.Shared.Physics;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.Maps;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.Components.Movement
{
    [RegisterComponent]
    public class ClimbModeComponent : SharedClimbModeComponent
    {
#pragma warning disable 649
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystem = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IServerNotifyManager _notifyManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
#pragma warning restore 649

        public override void Initialize()
        {
            base.Initialize();

            Owner.TryGetComponent(out ICollidableComponent body);
            _body = body;
        }

        private ICollidableComponent _body = default;
        private bool _isClimbing = false;

        public void SetClimbing(bool isClimbing)
        {
            _isClimbing = isClimbing;
            UpdateCollision();
            Dirty();
        }

        public void Update(float frameTime) 
        {
            if (_body != null && _isClimbing)
            {
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
