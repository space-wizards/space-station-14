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
        public bool IsBlocked => _numTicksBlocked > 5;

        /// <summary>
        /// If the controller is currently moving the player somewhere, it is considered active.
        /// </summary>
        public bool IsActive => _movingTo.HasValue;

        public void TryMoveTo(Vector2 from, Vector2 to)
        {
            if (ControlledComponent == null)
            {
                return;
            }

            _numTicksBlocked = 0;
            _lastKnownPosition = from;
            _movingTo = to;
        }

        public override void UpdateAfterProcessing()
        {
            base.UpdateAfterProcessing();

            if (ControlledComponent == null || _movingTo == null)
            {
                return;
            }

            if (ControlledComponent.Owner.Transform.WorldPosition.EqualsApprox(_lastKnownPosition, 0.01))
            {
                _numTicksBlocked++;
            }

            if (ControlledComponent.Owner.Transform.WorldPosition.EqualsApprox(_movingTo.Value, 0.01))
            {
                _movingTo = null;
            }

            if (_movingTo.HasValue)
            {
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
