
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Movement
{
    [RegisterComponent]
    public class MovementSpeedModifierComponent : Component
    {
        public override string Name => "MovementSpeedModifier";

        private float _cachedWalkSpeedModifier = 1.0f;
        public float WalkSpeedModifier
        {
            get
            {
                RecalculateMovementSpeedModifiers();
                return _cachedWalkSpeedModifier;
            }
        }
        private float _cachedSprintSpeedModifier;
        public float SprintSpeedModifier
        {
            get
            {
                RecalculateMovementSpeedModifiers();
                return _cachedSprintSpeedModifier;
            }
        }

        /// <summary>
        ///     set to warn us that a component's movespeed modifier has changed
        /// </summary>
        private bool _movespeedModifiersNeedRefresh = true;

        public void RefreshMovementSpeedModifiers()
        {
            _movespeedModifiersNeedRefresh = true;
        }

        /// <summary>
        ///     Recalculate movement speed with current modifiers, or return early if no change
        /// </summary>
        private void RecalculateMovementSpeedModifiers()
        {
            {
                if (!_movespeedModifiersNeedRefresh)
                    return;
                var movespeedModifiers = Owner.GetAllComponents<IMoveSpeedModifier>();
                float walkSpeedModifier = 1.0f;
                float sprintSpeedModifier = 1.0f;
                foreach (var component in movespeedModifiers)
                {
                    walkSpeedModifier *= component.WalkSpeedModifier;
                    sprintSpeedModifier *= component.SprintSpeedModifier;
                }
                _cachedWalkSpeedModifier = walkSpeedModifier;
                _cachedSprintSpeedModifier = sprintSpeedModifier;
            }
            _movespeedModifiersNeedRefresh = false;
        }
    }

    interface IMoveSpeedModifier
    {
        float WalkSpeedModifier { get; }
        float SprintSpeedModifier { get; }
    }
}
