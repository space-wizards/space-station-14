namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
/// Artifact that EMP
/// </summary>
[RegisterComponent]
public sealed class EmpArtifactComponent : Component
{
    [DataField("range")]
    public float Range = 4f;

    [DataField("energyConsumption")]
    public float EnergyConsumption = 1000000;
}