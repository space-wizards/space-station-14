using Content.Server.Anomaly.Effects;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Anomaly.Components;





[RegisterComponent, Access(typeof(LiquidAnomalySystem))]
public sealed partial class LiquidAnomalyComponent : Component
{

    /// <summary>
    /// The amount of reagent splashed out by the anomaly during the pulse.
    /// scales with Severity
    /// </summary>
    [DataField("maxSolutionPerPulse"), ViewVariables(VVAccess.ReadWrite)]
    public float MaxSolutionPerPulse = 30;

    /// <summary>
    /// Possible reagents for the anomaly.
    /// </summary>
    [DataField("possibleChemicals", required: true, customTypeSerializer: typeof(PrototypeIdListSerializer<ReagentPrototype>))]
    public List<string> PossibleChemicals = new List<string>();

    /// <summary>
    /// The maximum radius in which the anomaly injects reagents into the surrounding containers.
    /// </summary>
    [DataField("injectRadius"), ViewVariables(VVAccess.ReadWrite)]
    public float InjectRadius = 3;

    /// <summary>
    /// The name of the reagent that the anomaly produces.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("reagent", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
    public string Reagent = "Water";

}
