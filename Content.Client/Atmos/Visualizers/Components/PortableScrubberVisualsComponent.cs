namespace Content.Client.Atmos.Visualizers;

/// <summary>
/// Holds 2 pairs of states. The idle/running pair controls animation, while
/// the ready / full pair controls the color of the light.
/// </summary>
[RegisterComponent]
public sealed partial class PortableScrubberVisualsComponent : Component
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
