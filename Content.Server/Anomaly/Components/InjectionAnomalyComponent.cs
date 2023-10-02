using Content.Server.Anomaly.Effects;

namespace Content.Server.Anomaly.Components;

/// <summary>
/// This component allows the anomaly to create puddles from the solutionContainer
/// </summary>
[RegisterComponent, Access(typeof(InjectionAnomalySystem))]
public sealed partial class InjectionAnomalyComponent : Component
{
    /// <summary>
    /// the maximum amount of injection of a substance into an entity per pulsation
    /// scales with Severity
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxSolutionInjection = 15;
    /// <summary>
    /// the maximum amount of injection of a substance into an entity in the supercritical phase
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SuperCriticalSolutionInjection = 50;

    /// <summary>
    /// The maximum radius in which the anomaly injects reagents into the surrounding containers.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float InjectRadius = 3;
    /// <summary>
    /// The maximum radius in which the anomaly injects reagents into the surrounding containers.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float SuperCriticalInjectRadius = 15;

    /// <summary>
    /// The name of the prototype of the special effect that appears above the entities into which the injection was carried out
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public string VisualEffectPrototype = "PuddleSparkle";
    /// <summary>
    /// Solution name that can be drained.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("solution")]
    public string Solution { get; set; } = "default";
}
