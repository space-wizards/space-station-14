namespace Content.Client.Atmos.Visualizers;

/// <summary>
/// Holds the idle and running state for machines to control
/// playing animtions on the client.
/// </summary>
[RegisterComponent]
public sealed class PortableScrubberVisualsComponent : Component
{
    [DataField("idleState", required: true)]
    public string IdleState = default!;

    [DataField("runningState", required: true)]
    public string RunningState = default!;

    /// Powered and not full
    [DataField("readyState", required: true)]
    public string ReadyState = default!;

    /// Powered and full
    [DataField("fullState", required: true)]
    public string FullState = default!;
}
