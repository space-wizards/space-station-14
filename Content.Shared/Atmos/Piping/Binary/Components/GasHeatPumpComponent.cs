using Content.Shared.Atmos.Monitor.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Binary.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class GasHeatPumpComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = false;

    [DataField, AutoNetworkedField]
    public bool Blocked = false;

    [DataField]
    public string RegulatedName = "regulated";

    [DataField]
    public string ExternalName = "external";

    /// <summary>
    ///   Target tempfor the regulated pipe side
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TargetTemperature = Atmospherics.T20C;

    /// <summary>
    ///     Work term for the COP math, likely the same as the power draw
    /// </summary>
    [DataField]
    public float WorkInput = 5000f;

    /// <summary>
    ///     Hard cap on heat moved per second, whatever the temperatures
    /// </summary>
    [DataField]
    public float MaxHeatTransferRate = 14000f;

    /// <summary>
    ///     Fraction of ideal Carnot we actually hit, 1 = ideal
    /// </summary>
    [DataField]
    public float CarnotEfficiency = 0.65f;

    /// <summary>
    ///     Min pressure on either side before the pump enters Blocked state.
    /// </summary>
    [DataField]
    public float MinPressure = 0.5f;

    /// <summary>
    ///     Is either side above / below min or max temperature?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool TemperatureLocked = false;

    /// <summary>
    ///     Min temperature below which it locks out.
    /// </summary>
    [DataField]
    public float MinOperatingTemperature = Atmospherics.T0C - 100f;

    /// <summary>
    ///     Max temp above which it locks out
    /// </summary>
    [DataField]
    public float MaxOperatingTemperature = Atmospherics.T0C + 100f;
}

[Serializable, NetSerializable]
public sealed class GasHeatPumpData : IAtmosDeviceData
{
    public bool Enabled { get; set; }
    public bool Dirty { get; set; }
    public bool IgnoreAlarms { get; set; }
    public float TargetTemperature { get; set; }
    public float MinOperatingTemperature { get; set; }
    public float MaxOperatingTemperature { get; set; }
}
