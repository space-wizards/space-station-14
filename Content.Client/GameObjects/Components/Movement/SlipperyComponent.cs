#nullable enable
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
            if (!(curState is SlipperyComponentState state)) return;

            Slippery = state.Slippery;
            IntersectPercentage = state.IntersectPercentage;
            ParalyzeTime = state.ParalyzeTime;
            RequiredSlipSpeed = state.RequiredSlipSpeed;
            LaunchForwardsMultiplier = state.LaunchForwardsMultiplier;
        }
    }
}
