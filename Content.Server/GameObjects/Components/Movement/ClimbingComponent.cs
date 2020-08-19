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
using Robust.Shared.Physics;
using System.Diagnostics;

namespace Content.Server.GameObjects.Components.Movement
{
    [RegisterComponent]
    public class ClimbingComponent : SharedClimbingComponent, IActionBlocker
    {
        private bool _isClimbing = false;
        private ClimbController _climbController = default;

        public override bool IsClimbing
        {
            get
            {
                return _isClimbing;
            }
            set
            {
                if (!value && Body != null)
                {
                    Body.TryRemoveController<ClimbController>();
                }

                _isClimbing = value;
                Dirty();
            }
        }

        /// <summary>
        /// Make the owner climb from one point to another
        /// </summary>
        public void TryMoveTo(Vector2 from, Vector2 to)
        {
            if (Body != null)
            {
                _climbController = Body.EnsureController<ClimbController>();
                _climbController.TryMoveTo(from, to);
            }
        }

        public void Update(float frameTime) 
        {
            if (Body != null && IsClimbing)
            {
                if (_climbController != null && (_climbController.IsBlocked || !_climbController.IsActive))
                {
                    if (Body.TryRemoveController<ClimbController>())
                    {
                        _climbController = null;
                    }
                }

                if (!IsOnClimbableThisFrame && IsClimbing && _climbController == null)
                {
                    IsClimbing = false;
                }

                IsOnClimbableThisFrame = false;
            }
        }

        public override ComponentState GetComponentState()
        {
            return new ClimbModeComponentState(_isClimbing);
        }
    }
}
