#nullable enable
using System;
using Content.Shared.GameObjects.Components;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.StationEvents
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedRadiationPulseComponent))]
    public sealed class RadiationPulseComponent : SharedRadiationPulseComponent
    {
        private bool _draw;
        private bool _decay;
        private float _radsPerSecond;
        private float _range;
        private TimeSpan _endTime;

        public override float RadsPerSecond => _radsPerSecond;
        public override float Range => _range;
        public override TimeSpan EndTime => _endTime;
        public override bool Draw => _draw;
        public override bool Decay => _decay;

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not RadiationPulseState state)
            {
                return;
            }

            _radsPerSecond = state.RadsPerSecond;
            _range = state.Range;
            _draw = state.Draw;
            _decay = state.Decay;
            _endTime = state.EndTime;
        }
    }
}
