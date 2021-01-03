using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Shared.GameObjects.Components.Movement
{
    [RegisterComponent]
    public class MovementSpeedModifierComponent : Component
    {
        public const float DefaultBaseWalkSpeed = 4.0f;
        public const float DefaultBaseSprintSpeed = 7.0f;


        public override string Name => "MovementSpeedModifier";

        private float _cachedWalkSpeedModifier = 1.0f;
        [ViewVariables]
        public float WalkSpeedModifier
        {
            get
            {
                RecalculateMovementSpeedModifiers();
                return _cachedWalkSpeedModifier;
            }
        }
        private float _cachedSprintSpeedModifier;
        [ViewVariables]
        public float SprintSpeedModifier
        {
            get
            {
                RecalculateMovementSpeedModifiers();
                return _cachedSprintSpeedModifier;
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        [YamlField("baseWalkSpeed")]
        public float BaseWalkSpeed { get; set; } = 4;

        [ViewVariables(VVAccess.ReadWrite)]
        [YamlField("baseSprintSpeed")]
        public float BaseSprintSpeed { get; set; } = 7;

        [ViewVariables]
        public float CurrentWalkSpeed => WalkSpeedModifier * BaseWalkSpeed;
        [ViewVariables]
        public float CurrentSprintSpeed => SprintSpeedModifier * BaseSprintSpeed;

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

    public interface IMoveSpeedModifier
    {
        float WalkSpeedModifier { get; }
        float SprintSpeedModifier { get; }
    }
}
