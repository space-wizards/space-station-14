namespace Content.Server.Atmos.Piping.Unary.Components;

[RegisterComponent]
public sealed partial class HeatExchangerComponent : Component
{
    /// <summary>
    /// Name of the pipe node to exchange heat to.
    /// </summary>
    [DataField("inlet"), ViewVariables(VVAccess.ReadWrite)]
    public string InletName = "pipe";

    /// <summary>
    /// Pipe conductivity (mols/kPa/sec).
    /// </summary>
    [DataField("conductivity"), ViewVariables(VVAccess.ReadWrite)]
    public float G = 1f;

    /// <summary>
    /// Thermal convection coefficient (J/degK/sec).
    /// </summary>
    [DataField("convectionCoefficient"), ViewVariables(VVAccess.ReadWrite)]
    public float K = 8000f;

    /// <summary>
    /// Thermal radiation coefficient. Number of "effective" tiles this
    /// radiator radiates compared to superconductivity tile losses.
    /// </summary>
    [DataField("radiationCoefficient"), ViewVariables(VVAccess.ReadWrite)]
    public float alpha = 140f;
}

