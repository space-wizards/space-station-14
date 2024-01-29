using Robust.Shared.Serialization;

namespace Content.Server.Anomaly.Components;

/// <summary>
/// Hides some information about the anomaly when scanning it
/// </summary>
[RegisterComponent]
public sealed partial class SecretDataAnomalyComponent : Component
{
    [DataField]
    public int RandomStartSecretMin = 0;

    [DataField]
    public int RandomStartSecretMax = 0;

    [DataField]
    public List<AnomalySecretData> Secret = new();
}

[Serializable]
public enum AnomalySecretData : byte
{
    Severity,
    Stability,
    OutputPoint,
    ParticleDanger,
    ParticleUnstable,
    ParticleContainment,
    ParticleTransformation,
    Behaviour,
    Default
}
