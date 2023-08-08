using Robust.Shared.Serialization;

namespace Content.Shared.Light.Components;

/// <summary>
/// Handles station alert level and power changes for emergency lights.
/// All logic is serverside, animation is handled by <see cref="RotatingLightComponent"/>.
/// </summary>
public abstract class SharedEmergencyLightComponent : Component
{
}

[Serializable, NetSerializable]
public enum EmergencyLightVisuals
{
    On,
    Color
}

public enum EmergencyLightVisualLayers
{
    Base,
    LightOff,
    LightOn,
}
