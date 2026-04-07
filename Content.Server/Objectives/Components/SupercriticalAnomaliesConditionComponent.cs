namespace Content.Server.Objectives.Components;

[RegisterComponent]
public sealed partial class SupercriticalAnomaliesConditionComponent : Component
{
    /// <summary>
    /// The number of anomalies that have gone supercritical since this objective was added.
    /// </summary>
    [DataField]
    public int SupercriticalAnomalies = 0;
}