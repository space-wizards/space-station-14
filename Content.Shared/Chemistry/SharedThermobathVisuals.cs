using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry;

/// <summary>
/// Appearance data used to render a thermobath.
/// </summary>
[Serializable, NetSerializable]
public enum ThermobathVisuals : byte
{
    Powered,
    HasBeaker,
    ActiveMode
}

/// <summary>
/// Sprite layers controlled by the thermobath visualizer.
/// </summary>
public enum ThermobathVisualLayers : byte
{
    PowerOn,
    PowerOff,
    Heating,
    Cooling,
    Open,
    Beaker,
    LidIdle,
    LidCooling,
    LidHeating
}
