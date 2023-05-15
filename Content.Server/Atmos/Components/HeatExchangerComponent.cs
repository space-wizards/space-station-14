namespace Content.Server.Atmos.Components;

[RegisterComponent]
public sealed class HeatExchangerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("inlet")]
    public string InletName { get; set; } = "inlet";

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("outlet")]
    public string OutletName { get; set; } = "outlet";

    /// <summary>
    /// Pipe conductivity (mols/kPa/sec).
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("g")]
    public float G { get; set; } = 1f;

    /// <summary>
    /// Thermal convection coefficient (J/degK/sec).
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("k")]
    public float K { get; set; } = 8000f;

    /// <summary>
    /// Thermal radiation coefficient. Number of "effective" tiles this
    /// radiator radiates compared to superconductivity tile losses.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("alpha")]
    public float alpha { get; set; } = 400f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("debug")]
    public bool Debug { get; set; } = false;
}

