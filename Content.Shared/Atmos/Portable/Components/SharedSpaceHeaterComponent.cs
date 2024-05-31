using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Portable.Components;

[Serializable]
[NetSerializable]
public enum SpaceHeaterUiKey
{
    Key
}

[Serializable]
[NetSerializable]
public sealed class SpaceHeaterToggleMessage : BoundUserInterfaceMessage
{
}

[Serializable]
[NetSerializable]
public sealed class SpaceHeaterChangeTemperatureMessage : BoundUserInterfaceMessage
{
    public float Temperature { get; }

    public SpaceHeaterChangeTemperatureMessage(float temperature)
    {
        Temperature = temperature;
    }
}

[Serializable]
[NetSerializable]
public sealed class SpaceHeaterChangePowerLevelMessage : BoundUserInterfaceMessage
{
    public SpaceHeaterPowerLevel PowerLevel { get; }

    public SpaceHeaterChangePowerLevelMessage(SpaceHeaterPowerLevel powerLevel)
    {
        PowerLevel = powerLevel;
    }
}

[Serializable]
[NetSerializable]
public sealed class SpaceHeaterChangeModeMessage : BoundUserInterfaceMessage
{
    public SpaceHeaterMode Mode { get; }

    public SpaceHeaterChangeModeMessage(SpaceHeaterMode mode)
    {
        Mode = mode;
    }
}

[Serializable]
[NetSerializable]
public sealed class SpaceHeaterBoundUserInterfaceState : BoundUserInterfaceState
{
    public float MinTemperature { get; }
    public float MaxTemperature { get; }
    public float TargetTemperature { get; }
    public bool Enabled { get; }
    public SpaceHeaterMode Mode { get; }
    public SpaceHeaterPowerLevel PowerLevel { get; }

    public SpaceHeaterBoundUserInterfaceState(float minTemperature, float maxTemperature, float temperature, bool enabled, SpaceHeaterMode mode, SpaceHeaterPowerLevel powerLevel)
    {
        MinTemperature = minTemperature;
        MaxTemperature = maxTemperature;
        TargetTemperature = temperature;
        Enabled = enabled;
        Mode = mode;
        PowerLevel = powerLevel;
    }
}

[Serializable, NetSerializable]
public enum SpaceHeaterMode : byte
{
    Auto,
    Heat,
    Cool
}

[Serializable, NetSerializable]
public enum SpaceHeaterPowerLevel : byte
{
    Low,
    Medium,
    High
}
