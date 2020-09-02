#nullable enable
using System;
using Content.Shared.GameObjects.Components;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.StationEvents
{
    [RegisterComponent]
    public sealed class RadiationPulseComponent : SharedRadiationPulseComponent
    {
        public TimeSpan EndTime { get; private set; }

        public float Range { get; private set; } = 0f;

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (!(curState is RadiationPulseState state))
            {
                return;
            }

            EndTime = state.EndTime;
            Range = state.Range;
        }
    }
}
