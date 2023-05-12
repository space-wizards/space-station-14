using Content.Server.MachineLinking.Events;

namespace Content.Server.DeviceLinking.Components;

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

public enum SignalState
{
    Momentary, // Instantaneous pulse high, compatibility behavior
    Low,
    High
}

