using Content.Shared.GameObjects.Components.Movement;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Movement
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedSlipperyComponent))]
    public class SlipperyComponent : SharedSlipperyComponent
    {
        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not SlipperyComponentState state) return;

            _slippery = state.Slippery;
            _intersectPercentage = state.IntersectPercentage;
            _paralyzeTime = state.ParalyzeTime;
            _requiredSlipSpeed = state.RequiredSlipSpeed;
            _launchForwardsMultiplier = state.LaunchForwardsMultiplier;
        }
    }
}
