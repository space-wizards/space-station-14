using Robust.Shared.Serialization;
using Robust.Shared.GameStates;

namespace Content.Shared.RadioJammer;

[NetworkedComponent]

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