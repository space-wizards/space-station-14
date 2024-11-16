namespace Content.Server.Xenoarchaeology.Artifact.XAE.Components;

/// <summary>
/// Effect of EMP on activation.
/// </summary>
[RegisterComponent, Access(typeof(XAEEmpInAreaSystem))]
public sealed partial class XAEEmpInAreaComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Range = 4f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float EnergyConsumption = 1000000;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float DisableDuration = 60f;
}
