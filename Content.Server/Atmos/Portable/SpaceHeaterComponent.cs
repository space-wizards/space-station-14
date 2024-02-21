using Content.Shared.Atmos;
using Content.Shared.Atmos.Piping.Portable.Components;
using Content.Shared.Atmos.Visuals;

namespace Content.Server.Atmos.Portable;

[RegisterComponent]
public sealed partial class SpaceHeaterComponent : Component
{
    /// <summary>
    /// Current mode the space heater is in. Possible values : Auto, Heat and Cool
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SpaceHeaterMode Mode = SpaceHeaterMode.Auto;

    /// <summary>
    /// The state the space heater is in. Possible values : Off, StandBy, Heating, Cooling
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SpaceHeaterState State = SpaceHeaterState.Off;

    /// <summary>
    /// Maximum target temperature the device can be set to
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxTemperature = Atmospherics.T20C + 20;

    /// <summary>
    /// Minimal target temperature the device can be set to
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MinTemperature = Atmospherics.T0C - 10;

    /// <summary>
    ///     Coefficient of performance. Output power / input power.
    ///     Positive for heaters, negative for freezers.
    /// </summary>
    [DataField("heatingCoefficientOfPerformance")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float HeatingCp = 1f;

    [DataField("coolingCoefficientOfPerformance")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float CoolingCp = -0.8f;

    /// <summary>
    ///     The temperature delta from the target temperature after which the space heater switch mode while in Auto. Value should account for the thermomachine temperature tolerance.
    /// </summary>
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float AutoModeSwitchThreshold = 0.8f;
}
