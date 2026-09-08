namespace Content.Server.Objectives.Components;

/// <summary>
/// Objective condition that requires a certain number of anomalies (defined by <see cref="NumberObjectiveComponent"/>) to go supercritical while the objective is in play.
/// </summary>
[RegisterComponent]
public sealed partial class SupercriticalAnomaliesConditionComponent : Component
{
    /// <summary>
    /// The number of anomalies that have gone supercritical since this objective was added.
    /// </summary>
    [DataField]
    public int SupercriticalAnomalies = 0;
}
