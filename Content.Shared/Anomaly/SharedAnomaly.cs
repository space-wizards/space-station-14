using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Anomaly;

[Serializable, NetSerializable]
public enum AnomalyVisuals : byte
{
    IsPulsing,
    Supercritical
}

[Serializable, NetSerializable]
public enum AnomalyVisualLayers : byte
{
    Base,
    Animated
}

/// <summary>
/// The types of anomalous particles used
/// for interfacing with anomalies.
/// </summary>
/// <remarks>
/// The only thought behind these names is that
/// they're a continuation of radioactive particles.
/// Yes i know detla+ waves exist, but they're not
/// common enough for me to care.
/// </remarks>
[Serializable, NetSerializable]
public enum AnomalousParticleType : byte
{
    Delta,
    Epsilon,
    Zeta,
    Default
}

[Serializable, NetSerializable]
public enum AnomalyVesselVisuals : byte
{
    HasAnomaly,
    AnomalyState
}

[Serializable, NetSerializable]
public enum AnomalyVesselVisualLayers : byte
{
    Base
}

[Serializable, NetSerializable]
public enum AnomalyGeneratorVisuals : byte
{
    Generating
}

[Serializable, NetSerializable]
public enum AnomalyGeneratorVisualLayers : byte
{
    Base
}

[Serializable, NetSerializable]
public enum AnomalyScannerUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class AnomalyScannerUserInterfaceState : BoundUserInterfaceState
{
    public FormattedMessage Message;

    public TimeSpan? NextPulseTime;

    public AnomalyScannerUserInterfaceState(FormattedMessage message, TimeSpan? nextPulseTime)
    {
        Message = message;
        NextPulseTime = nextPulseTime;
    }
}

[Serializable, NetSerializable]
public enum AnomalyGeneratorUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class AnomalyGeneratorUserInterfaceState : BoundUserInterfaceState
{
    public TimeSpan CooldownEndTime;

    public int FuelAmount;

    public int FuelCost;

    public AnomalyGeneratorUserInterfaceState(TimeSpan cooldownEndTime, int fuelAmount, int fuelCost)
    {
        CooldownEndTime = cooldownEndTime;
        FuelAmount = fuelAmount;
        FuelCost = fuelCost;
    }
}

[Serializable, NetSerializable]
public sealed class AnomalyGeneratorGenerateButtonPressedEvent : BoundUserInterfaceMessage
{

}
