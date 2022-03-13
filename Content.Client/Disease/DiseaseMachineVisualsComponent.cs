namespace Content.Client.Disease;

/// <summary>
/// Holds the idle and running state for machines to control
/// playing animtions on the client.
/// </summary>
[RegisterComponent]
public sealed class DiseaseMachineVisualsComponent : Component
{
    [DataField("idleState", required: true)]
    public string IdleState = default!;

    [DataField("runningState", required: true)]
    public string RunningState = default!;
}
