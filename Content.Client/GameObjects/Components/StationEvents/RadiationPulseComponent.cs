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
        private float _dps;
        private float _range;
        private TimeSpan _endTime;

        public override float DPS
        {
            get => _dps;
            set
            {
                _dps = value;
                Dirty();
            }
        }

        public override float Range
        {
            get => _range;
            set
            {
                _range = value;
                Dirty();
            }
        }

        public override TimeSpan EndTime => _endTime;

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (!(curState is RadiationPulseState state))
            {
                return;
            }

            _dps = state.DPS;
            _range = state.Range;
            _endTime = state.EndTime;
        }
    }
}
