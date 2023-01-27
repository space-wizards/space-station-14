namespace Content.Shared.Anomaly.Effects.Components;

[RegisterComponent]
public sealed class ElectricityAnomalyComponent : Component
{
    [DataField("maxElectrocutionRange"), ViewVariables(VVAccess.ReadWrite)]
    public float MaxElectrocuteRange = 6f;

    [DataField("maxElectrocuteDamage"), ViewVariables(VVAccess.ReadWrite)]
    public float MaxElectrocuteDamage = 20f;

    [DataField("maxElectrocuteDuration"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan MaxElectrocuteDuration = TimeSpan.FromSeconds(8);
}
