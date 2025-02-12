using Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Systems;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Effects.Components;

/// <summary>
/// Artifact that EMP
/// </summary>
[RegisterComponent]
[Access(typeof(EmpArtifactSystem))]
public sealed partial class EmpArtifactComponent : Component
{
    [DataField, ViewVariables]
    public float Range = 4f;

    [DataField, ViewVariables]
    public float EnergyConsumption = 1000000;

    [DataField, ViewVariables]
    public float DisableDuration = 60f;
}
