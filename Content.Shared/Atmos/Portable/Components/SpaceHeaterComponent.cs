using Content.Shared.Atmos.Portable;
using Content.Shared.Guidebook;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Portable.Components;

[Access(typeof(SharedSpaceHeaterSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class SpaceHeaterComponent : Component
{
    /// <summary>
    /// Current mode the space heater is in. Possible values : Auto, Heat and Cool
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public SpaceHeaterMode Mode = SpaceHeaterMode.Auto;

    /// <summary>
    /// The power level the space heater is currently set to. Possible values : Low, Medium, High
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public SpaceHeaterPowerLevel PowerLevel = SpaceHeaterPowerLevel.Medium;

    /// <summary>
    /// Maximum target temperature the device can be set to
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [GuidebookData]
    public float MaxTemperature = Atmospherics.T20C + 20;

    /// <summary>
    /// Minimal target temperature the device can be set to
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    [GuidebookData]
    public float MinTemperature = Atmospherics.T0C - 10;

    /// <summary>
    /// Coefficient of performance. Output power / input power.
    /// Positive for heaters, negative for freezers.
    /// </summary>
    [DataField("heatingCoefficientOfPerformance")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float HeatingCp = 1f;

    [DataField("coolingCoefficientOfPerformance")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float CoolingCp = -0.9f;

    /// <summary>
    /// The delta from the target temperature after which the space heater switch mode while in Auto. Value should account for the thermomachine temperature tolerance.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float AutoModeSwitchThreshold = 0.8f;

    /// <summary>
    /// Current electrical power consumption, in watts, of the space heater at medium power level. Passed to the thermomachine component.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float PowerConsumption = 3500f;
}

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
    public bool IsPowerDisabled { get; }
    public SpaceHeaterToggleMessage(bool isPowerDisabled)
    {
        IsPowerDisabled = isPowerDisabled;
    }

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
