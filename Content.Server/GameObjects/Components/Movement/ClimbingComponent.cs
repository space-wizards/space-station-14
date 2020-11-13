#nullable enable
using Content.Shared.GameObjects.Components.Buckle;
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.GameObjects.Components.Movement
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedClimbingComponent))]
    public class ClimbingComponent : SharedClimbingComponent
    {
        private bool _isClimbing;
        private ClimbController? _climbController;

        public override bool IsClimbing
        {
            get => _isClimbing;
            set
            {
                if (_isClimbing == value)
                    return;

                if (!value)
                {
                    Body?.TryRemoveController<ClimbController>();
                }

                _isClimbing = value;
                Dirty();
            }
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case BuckleMessage msg:
                    if (msg.Buckled)
                        IsClimbing = false;

                    break;
            }
        }

        /// <summary>
        /// Make the owner climb from one point to another
        /// </summary>
        public void TryMoveTo(Vector2 from, Vector2 to)
        {
            if (Body == null)
                return;

            _climbController = Body.EnsureController<ClimbController>();
            _climbController.TryMoveTo(from, to);
        }

        public void Update()
        {
            if (!IsClimbing || Body == null)
                return;

            if (_climbController != null && (_climbController.IsBlocked || !_climbController.IsActive))
            {
                if (Body.TryRemoveController<ClimbController>())
                {
                    _climbController = null;
                }
            }

            if (IsClimbing)
                Body.WakeBody();

            if (!IsOnClimbableThisFrame && IsClimbing && _climbController == null)
                IsClimbing = false;

            IsOnClimbableThisFrame = false;
        }

        public override ComponentState GetComponentState()
        {
            return new ClimbModeComponentState(_isClimbing);
        }
    }
}
