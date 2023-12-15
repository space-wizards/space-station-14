using Robust.Shared.Serialization;

namespace Content.Shared.Power.Generation.Teg;

/// <summary>
/// Appearance keys for the TEG &amp; its circulators.
/// </summary>
[Serializable, NetSerializable]
public enum TegVisuals
{
    PowerOutput,
    CirculatorSpeed,
    CirculatorPower,
}

/// <summary>
/// Visual sprite layers for the TEG &amp; its circulators.
/// </summary>
[Serializable, NetSerializable]
public enum TegVisualLayers
{
    PowerOutput,
    CirculatorBase,
    CirculatorLight
}

/// <summary>
/// Visual speed levels for the TEG circulators.
/// </summary>
[Serializable, NetSerializable]
public enum TegCirculatorSpeed
{
    SpeedStill,
    SpeedSlow,
    SpeedFast
}
