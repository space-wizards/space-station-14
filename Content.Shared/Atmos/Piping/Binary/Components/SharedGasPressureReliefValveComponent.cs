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
    /// Gets the threshold pressure of the valve.
    /// </summary>
    public float ThresholdPressure { get; }

    /// <summary>
    /// The current flow rate of the valve in L/S.
    /// </summary>
    public float FlowRate { get; }

    /// <summary>
    /// Whether the valve is opened or closed.
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="GasPressureReliefValveBoundUserInterfaceState"/> class.
    /// </summary>
    /// <param name="thresholdPressure">The threshold pressure of the valve.</param>
    /// <param name="flowRate">The current flow rate of the valve in L/S.</param>
    /// <param name="enabled">The current position of the valve.</param>
    public GasPressureReliefValveBoundUserInterfaceState(float thresholdPressure, float flowRate, bool enabled)
    {
        // The title of the window is provided by the identity system providing the label
        // of the valve. This is done on every UI update. Hence, why we don't have a title here.
        ThresholdPressure = thresholdPressure;
        FlowRate = flowRate;
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
