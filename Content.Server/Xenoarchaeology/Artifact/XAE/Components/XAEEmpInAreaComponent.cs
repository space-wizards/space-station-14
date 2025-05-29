namespace Content.Server.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
/// Effect of EMP on activation.
/// </summary>
[RegisterComponent, Access(typeof(XAEEmpInAreaSystem))]
public sealed partial class XAEEmpInAreaComponent : Component
{
    /// <summary>
    /// Range of EMP effect.
    /// </summary>
    [DataField]
    public float Range = 4f;

    /// <summary>
    /// Energy to be consumed from energy containers.
    /// </summary>
    [DataField]
    public float EnergyConsumption = 1000000;

    /// <summary>
    /// Duration (in seconds) for which devices going to be disabled.
    /// </summary>
    [DataField]
    public float DisableDuration = 60f;
}
