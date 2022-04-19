namespace Content.Client.Lathe;

/// <summary>
/// Holds the idle and running state for machines to control
/// playing animations on the client.
/// </summary>
[RegisterComponent]
public sealed class LatheVisualsComponent : Component
{
    [DataField("idleState", required: true)]
    public string IdleState = default!;

    [DataField("runningState", required: true)]
    public string RunningState = default!;
    
    [ViewVariables]
    [DataField("ignoreColor")]
    public bool IgnoreColor;
}
