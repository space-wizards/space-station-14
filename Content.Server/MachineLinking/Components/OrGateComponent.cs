using Content.Server.MachineLinking.Events;
using Content.Shared.MachineLinking;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.MachineLinking.Components;

[RegisterComponent]
public sealed class OrGateComponent : Component
{
    // Initial state
    [ViewVariables]
    public SignalState StateA1 = SignalState.Low;

    [ViewVariables]
    public SignalState StateB1 = SignalState.Low;

    [ViewVariables]
    public SignalState LastO1 = SignalState.Low;

    [ViewVariables]
    public SignalState StateA2 = SignalState.Low;

    [ViewVariables]
    public SignalState StateB2 = SignalState.Low;

    [ViewVariables]
    public SignalState LastO2 = SignalState.Low;
}
