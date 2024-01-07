using Content.Server.Anomaly.Effects;
using Robust.Shared.Prototypes;

namespace Content.Server.Anomaly.Components;

/// <summary>
/// This component allows the anomaly to inject liquid from the SolutionContainer
/// into the surrounding entities with the InjectionSolution component
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
    public EntProtoId VisualEffectPrototype = "PuddleSparkle";
    /// <summary>
    /// Solution name that can be drained.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string Solution { get; set; } = "default";
}
