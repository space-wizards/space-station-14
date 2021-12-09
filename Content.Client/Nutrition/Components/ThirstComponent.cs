using Content.Shared.Movement.Components;
using Content.Shared.Movement.EntitySystems;
using Content.Shared.Nutrition.Components;
using Robust.Shared.GameObjects;

namespace Content.Client.Nutrition.Components
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

            EntitySystem.Get<MovementSpeedModifierSystem>().RefreshMovementSpeedModifiers(Owner);
        }
    }
}
