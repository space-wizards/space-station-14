using Robust.Shared.Serialization;

namespace Content.Shared.Anomaly;

/// <summary>
/// The types of anomalous particles used
/// for interfacing with anomalies.
/// </summary>
[Serializable, NetSerializable]
public enum AnomalousParticleTypes : byte
{
    Alpha,
    Beta,
    Gamma
}

[Serializable, NetSerializable]
public enum AnomalyVesselVisuals : byte
{
    HasAnomaly
}
[Serializable, NetSerializable]
public enum AnomalyVesselVisualLayers : byte
{
    Base
}

