namespace Content.Client.Conveyor.Components;

[RegisterComponent]
public sealed class ConveyorVisualsComponent : Component
{
    [DataField("state_running")]
    public string? StateRunning;

    [DataField("state_stopped")]
    public string? StateStopped;

    [DataField("state_reversed")]
    public string? StateReversed;
}
