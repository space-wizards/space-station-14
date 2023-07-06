namespace Content.Server.Chemistry.Components.SolutionManager;

/// <summary>
/// Lets the solution conduct heat to/from atmos gases.
/// </summary>
[RegisterComponent]
public sealed class SolutionGasHeatConductivityComponent : Component
{
    /// <summary>
    /// Solution that conducts heat.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("solution")]
    public string Solution { get; set; } = "default";

    /// <summary>
    /// The heat conductivity between the gas and solution.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("wattsPerKelvin")]
    public float WattsPerKelvin = 1;
}
