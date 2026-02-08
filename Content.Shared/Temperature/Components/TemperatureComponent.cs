using Content.Shared.Temperature.HeatContainers;

namespace Content.Shared.Temperature.Components;

/// <summary>
/// Handles changing temperature,
/// informing others of the current temperature.
/// </summary>
[RegisterComponent]
public sealed partial class TemperatureComponent : Component
{
    /// <summary>
    /// Surface temperature which is modified by the environment.
    /// </summary>
    [DataField]
    public HeatContainer HeatContainer = new ();

    /// <summary>
    /// The specific heat capacity of this entity in J/(kg*K). Humans are about 3kJ/kg
    /// </summary>
    [DataField]
    public float SpecificHeat = 3000f;

    /// <summary>
    /// Easy access for the current temperature of the entity.
    /// </summary>
    [ViewVariables]
    public float CurrentTemperature => HeatContainer.Temperature;

    /// <summary>
    /// Easy access for the current heat capacity of the entity.
    /// </summary>
    [ViewVariables]
    public float HeatCapacity => HeatContainer.HeatCapacity;

    /// <summary>
    /// Thermal Conductance in W/K.
    /// Human skin is about 0.3 W/(m*K) and the body has about 2m^2 of surface area.
    /// Divide that by the thickness of skin of about 2mm giving us a final value of 300
    /// Source: https://pmc.ncbi.nlm.nih.gov/articles/PMC8953946/
    /// </summary>
    [DataField]
    public float ThermalConductance = 300f;
}
