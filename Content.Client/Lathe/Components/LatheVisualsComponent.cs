namespace Content.Client.Lathe;

/// <summary>
/// Holds the idle and running state for machines to control
/// playing animtions on the client.
/// </summary>
[RegisterComponent]
public sealed class LatheVisualsComponent : Component
{
    [DataField("idleState", required: true)]
    public string IdleState = default!;

    [DataField("runningState", required: true)]
    public string RunningState = default!;

    [DataField("insertingState")]
    public string InsertingState = default!;

    // This visualizer is pretty generic and not everything has
    // any inserting animations at all.
    [DataField("hasInsertingAnims")]
    public bool HasInsertingAnims = true;
}
