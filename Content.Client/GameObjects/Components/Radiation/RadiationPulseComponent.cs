#nullable enable
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Radiation
{
    [ComponentReference(typeof(SharedRadiationPulseComponent))]
    public abstract class RadiationPulseComponent : SharedRadiationPulseComponent
    {
        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not RadiationPulseAnomalyState state)
            {
                return;
            }

            Range = state.Range;
            StartTime = state.StartTime;
            EndTime = state.EndTime;
        }
    }

    [RegisterComponent]
    [ComponentReference(typeof(RadiationPulseComponent))]
    public sealed class RadiationPulseAnomaly : RadiationPulseComponent
    {
        public override string Name => "RadiationPulseAnomaly";
        public override uint? NetID => ContentNetIDs.RADIATION_PULSE;
    }

    [RegisterComponent]
    [ComponentReference(typeof(RadiationPulseComponent))]
    public sealed class RadiationPulseSingularity : RadiationPulseComponent
    {
        public override string Name => "RadiationPulseSingularity";
    }
}
