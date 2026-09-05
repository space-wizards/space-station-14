using Content.Shared.Temperature.Systems;

namespace Content.Shared.Temperature.Components;

/// <summary>
/// Converts a part of the electrical power provided by an APC to this entity, into thermal energy and distributes it within the entity.
/// </summary>
[RegisterComponent]
public sealed partial class ElectricalHeaterComponent : Component
{
    /// <summary>
    /// The minimum power needed to run this device. below we assume standby.
    /// </summary>
    [DataField]
    public float MinimumPower { get; set; } = 0.125f;

    /// <summary>
    /// Temperature output is (power drawn - offset) * efficiency
    /// set higher than 0 to simulate a device doing other things than just heating stuff.
    /// </summary>
    [DataField]
    public uint Offset { get; set; } = 0;

    /// <summary>
    /// How much electricity is converted to heat.
    /// Pick negative value if you wish to freeze instead.
    /// </summary>
    [DataField]
    public float Efficiency { get; set; } = 1;

    /// <summary>
    /// Where does heat end up in the entity?
    /// </summary>
    [DataField]
    public HeatContainerQuerySystem.HeatContainerAddress[] DistributeHeatTo { get; set; } = [];

    /// <summary>
    /// if set will stop heating/cooling stuff at limit.
    /// </summary>
    [DataField]
    public float? TemperatureLimit { get; set; }

    /// <summary>
    /// Goes to standby if empty?
    /// </summary>
    [DataField]
    public bool CanStandbyIfEmpty { get; set; } = true;

    /// <summary>
    /// Displays if power is needed or if can go in standby because of temperature limit/emptiness.
    /// </summary>
  [DataField]
    public bool IsStandby { get; set; } = false;


}
