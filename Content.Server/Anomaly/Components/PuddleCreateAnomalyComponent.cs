using Content.Server.Anomaly.Effects;

namespace Content.Server.Anomaly.Components;

/// <summary>
/// This component allows the anomaly to create puddles from the solutionContainer
/// </summary>
[RegisterComponent, Access(typeof(PuddleCreateAnomalySystem))]
public sealed partial class PuddleCreateAnomalyComponent : Component
{
    /// <summary>
    /// The maximum amount of solution that an anomaly can splash out of the storage on the floor during pulsation.
    /// Scales with Severity.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxPuddleSize = 100;

    /// <summary>
    /// The maximum amount of solution that an anomaly can splash out of the storage on the floor during supercritical event
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SuperCriticalPuddleSize = 1000;

    /// <summary>
    /// Solution name that can be drained.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Solution { get; set; } = "default";
}
