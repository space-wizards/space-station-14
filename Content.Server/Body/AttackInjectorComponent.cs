using Content.Server.Anomaly.Effects;

namespace Content.Server.Body;

/// <summary>
/// This component allows the creature to inject reagent from the specified SolutionStogate
/// into the target during an attack
/// </summary>
[RegisterComponent, Access(typeof(AttackInjectorSystem))]
public sealed partial class AttackInjectorComponent : Component
{
    /// <summary>
    /// The number of reagent transferred from storage to the target's circulatory system
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float GivingInjectionValue;

    /// <summary>
    /// The number of reagent transferred from target's circulatory system to the storage
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ReceiveInjectionValue;

    /// <summary>
    /// from which storage will the liquid be pumped out
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public string DrainSolution { get; set; } = "bloodstream";

    /// <summary>
    /// in which storage will the reagent be poured
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public string StorageSolution { get; set; } = "chemicals";
}
