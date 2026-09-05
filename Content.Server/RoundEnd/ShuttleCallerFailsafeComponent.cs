namespace Content.Server.RoundEnd;

/// <summary>
///     Given to a grid.
///     If no shuttle callers (marked by <see cref="ShuttleCallerComponent"/>) are present, call evac.
/// </summary>
[RegisterComponent]
public sealed partial class ShuttleCallerFailsafeComponent : Component
{
    /// <summary>
    ///     If shuttle callers present on the same map are also counted.
    ///     If not, only count shuttle callers on the grid.
    /// </summary>
    [DataField]
    public bool IncludeCallersInSameMap = false;

    /// <summary>
    ///     Any shuttle caller currently counted.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> Callers = new();
}
