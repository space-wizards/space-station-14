namespace Content.Client.Disease;

[RegisterComponent]
public sealed class DiseaseMachineVisualsComponent : Component
{
    [DataField("idleState", required: true)]
    public string IdleState = default!;

    [DataField("runningState", required: true)]
    public string RunningState = default!;
}
