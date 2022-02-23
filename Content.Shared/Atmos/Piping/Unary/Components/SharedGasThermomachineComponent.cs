using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Unary.Components;

[Serializable]
[NetSerializable]
public enum ThermomachineUiKey
{
    Key
}

[Serializable]
[NetSerializable]
public enum ThermoMachineMode : byte
{
    Freezer = 0,
    Heater = 1,
}

[Serializable]
[NetSerializable]
public sealed class GasThermomachineToggleMessage : BoundUserInterfaceMessage
{
}

[Serializable]
[NetSerializable]
public sealed class GasThermomachineChangeTemperatureMessage : BoundUserInterfaceMessage
{
    public float Temperature { get; }

    public GasThermomachineChangeTemperatureMessage(float temperature)
    {
        Temperature = temperature;
    }
}

[Serializable]
[NetSerializable]
public sealed class GasThermomachineBoundUserInterfaceState : BoundUserInterfaceState
{
    public float MinTemperature { get; }
    public float MaxTemperature { get; }
    public float Temperature { get; }
    public bool Enabled { get; }
    public ThermoMachineMode Mode { get; }

    public GasThermomachineBoundUserInterfaceState(float minTemperature, float maxTemperature, float temperature, bool enabled, ThermoMachineMode mode)
    {
        MinTemperature = minTemperature;
        MaxTemperature = maxTemperature;
        Temperature = temperature;
        Enabled = enabled;
        Mode = mode;
    }
}
