using Content.Shared.GameObjects.Components.Movement;
using Content.Shared.GameObjects.Components.Nutrition;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Nutrition
{
    [RegisterComponent]
    public class HungerComponent : SharedHungerComponent
    {
        private HungerThreshold _currentHungerThreshold;
        public override HungerThreshold CurrentHungerThreshold => _currentHungerThreshold;

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not HungerComponentState hunger)
            {
                return;
            }

            _currentHungerThreshold = hunger.CurrentThreshold;

            if (Owner.TryGetComponent(out MovementSpeedModifierComponent? movement))
            {
                movement.RefreshMovementSpeedModifiers();
            }
        }
    }
}
