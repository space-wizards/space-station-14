using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Binary.Components;

/// <summary>
/// Represents the unique key for the UI.
/// </summary>
[Serializable, NetSerializable]
public enum GasPressureRegulatorUiKey : byte
{
    Key,
}

/// <summary>
/// Message sent to change the pressure threshold of the gas pressure regulator.
/// </summary>
/// <param name="pressure">The new pressure threshold value.</param>
[Serializable, NetSerializable]
public sealed class GasPressureRegulatorChangeThresholdMessage(float pressure) : BoundUserInterfaceMessage
{
    /// <summary>
    /// Gets the new threshold pressure value.
    /// </summary>
    public float ThresholdPressure { get; } = pressure;
}
