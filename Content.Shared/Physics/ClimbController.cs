#nullable enable
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Content.Shared.Physics
{
    /// <summary>
    /// Movement controller used by the climb system. Lerps the player from A to B.
    /// Also does checks to make sure the player isn't blocked.
    /// </summary>
    public class ClimbController : VirtualController
    {
        private Vector2? _movingTo = null;
        private Vector2 _lastKnownPosition = default;
        private int _numTicksBlocked = 0;

        /// <summary>
        /// If 5 ticks have passed and our position has not changed then something is blocking us.
        /// </summary>
        public bool IsBlocked => _numTicksBlocked > 5 || _isMovingWrongDirection;

        /// <summary>
        /// If the controller is currently moving the player somewhere, it is considered active.
        /// </summary>
        public bool IsActive => _movingTo.HasValue;

        private float _initialDist = default;
        private bool _isMovingWrongDirection = false;

        public void TryMoveTo(Vector2 from, Vector2 to)
        {
            if (ControlledComponent == null)
            {
                return;
            }

            _initialDist = (from - to).Length;
            _numTicksBlocked = 0;
            _lastKnownPosition = from;
            _movingTo = to;
            _isMovingWrongDirection = false;
        }

        public override void UpdateAfterProcessing()
        {
            base.UpdateAfterProcessing();

            if (ControlledComponent == null || _movingTo == null)
            {
                return;
            }

            ControlledComponent.WakeBody();

            if ((ControlledComponent.Owner.Transform.WorldPosition - _lastKnownPosition).Length <= 0.05f)
            {
                _numTicksBlocked++;
            }
            else
            {
                _numTicksBlocked = 0;
            }

            _lastKnownPosition = ControlledComponent.Owner.Transform.WorldPosition;

            if ((ControlledComponent.Owner.Transform.WorldPosition - _movingTo.Value).Length <= 0.1f) 
            {
                _movingTo = null;
            }

            if (_movingTo.HasValue)
            {
                var dist = (_lastKnownPosition - _movingTo.Value).Length;

                if (dist > _initialDist)
                {
                    _isMovingWrongDirection = true;
                }

                var diff = _movingTo.Value - ControlledComponent.Owner.Transform.WorldPosition;
                LinearVelocity = diff.Normalized * 5;
            }
            else
            {
                LinearVelocity = Vector2.Zero;
            }
        }
    }
}
