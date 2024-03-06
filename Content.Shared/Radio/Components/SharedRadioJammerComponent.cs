using Robust.Shared.Serialization;

namespace Content.Shared.RadioJammer;

public abstract partial class SharedRadioJammerComponent : Component
{
}


[Serializable, NetSerializable]
public enum RadioJammerChargeLevel : byte
{
    Low,
    Medium,
    High
}

[Serializable, NetSerializable]
public enum RadioJammerLayers : byte
{
    LED
}

[Serializable, NetSerializable]
public enum RadioJammerVisuals : byte
{
    ChargeLevel,
    LEDOn
}