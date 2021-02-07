#nullable enable
using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.Components.Nutrition;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Nutrition
{
    [RegisterComponent]
    public class ThirstComponent : SharedThirstComponent
    {
        private ThirstThreshold _currentThirstThreshold;
        public override ThirstThreshold CurrentThirstThreshold => _currentThirstThreshold;

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not ThirstComponentState thirst)
            {
                return;
            }

            _currentThirstThreshold = thirst.CurrentThreshold;

            if (Owner.TryGetComponent(out MovementSpeedModifierComponent? movement))
            {
                movement.RefreshMovementSpeedModifiers();
            }
        }
    }
}
