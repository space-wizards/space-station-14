using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Robust.Shared.GameObjects;

namespace Content.Client.Nutrition.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedHungerComponent))]
    public sealed class HungerComponent : SharedHungerComponent
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
        }
    }
}
