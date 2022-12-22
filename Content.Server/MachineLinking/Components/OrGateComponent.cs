using Content.Server.MachineLinking.Events;
using Content.Shared.MachineLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.MachineLinking.Components
{
    [RegisterComponent]
    public sealed class OrGateComponent : Component
    {
        // Initial state
        public SignalState stateA1 = SignalState.Low;
        public SignalState stateB1 = SignalState.Low;
        public SignalState lastO1 = SignalState.Low;

        public SignalState stateA2 = SignalState.Low;
        public SignalState stateB2 = SignalState.Low;
        public SignalState lastO2 = SignalState.Low;
    }
}
