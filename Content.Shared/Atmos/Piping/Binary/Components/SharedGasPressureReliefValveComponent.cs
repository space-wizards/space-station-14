using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Binary.Components;

/// <summary>
/// Represents the unique key for the UI.
/// </summary>
[Serializable, NetSerializable]
public enum GasPressureReliefValveUiKey : byte
{
    Key,
}

/// <summary>
/// Represents the state of the Gas Pressure Relief Valve user interface.
/// </summary>
[Serializable, NetSerializable]
public sealed class GasPressureReliefValveBoundUserInterfaceState : BoundUserInterfaceState
{
    /// <summary>
    /// Gets the label of the valve.
    /// </summary>
    public string ValveLabel { get; }

    /// <summary>
    /// Gets the current transfer rate of the valve.
    /// </summary>
    public float TransferRate { get; }

    /// <summary>
    /// Gets a value indicating whether the valve is currently enabled (open or closed).
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// Gets the threshold pressure of the valve.
    /// </summary>
    public float ThresholdPressure { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GasPressureReliefValveBoundUserInterfaceState"/> class.
    /// </summary>
    /// <param name="valveLabel">The label of the valve.</param>
    /// <param name="transferRate">The transfer rate of the valve.</param>
    /// <param name="thresholdPressure">The threshold pressure of the valve.</param>
    /// <param name="enabled">Indicates whether the valve is enabled.</param>
    public GasPressureReliefValveBoundUserInterfaceState(string valveLabel,
        float transferRate,
        float thresholdPressure,
        bool enabled)
    {
        ValveLabel = valveLabel;
        TransferRate = transferRate;
        ThresholdPressure = thresholdPressure;
        Enabled = enabled;
    }
}

/// <summary>
/// Message sent to change the pressure threshold of the Gas Pressure Relief Valve.
/// </summary>
/// <param name="pressure">The new pressure threshold value.</param>
[Serializable, NetSerializable]
public sealed class GasPressureReliefValveChangeThresholdMessage(float pressure) : BoundUserInterfaceMessage
{
    /// <summary>
    /// Gets the new threshold pressure value.
    /// </summary>
    public float ThresholdPressure { get; } = pressure;
}
