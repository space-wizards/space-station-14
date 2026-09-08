using Content.Shared.Atmos;
using Content.Shared.Temperature.HeatContainer;
using Content.Shared.Temperature.Systems;

namespace Content.Shared.Temperature.Components;

/// <summary>
/// Handles changing temperature,
/// informing others of the current temperature.
/// </summary>
[RegisterComponent]
[Access(typeof(SharedTemperatureSystem))]
public sealed partial class TemperatureComponent : Component, IHeatContainer
{
    /// <summary>
    /// The specific heat capacity of this entity in J/(kg*K). Humans are about 3kJ/(kg*K)
    /// </summary>
    [DataField]
    public float SpecificHeat = 3000f;

    [DataField]
    public float HeatCapacity { get; set; }

    [DataField]
    public float Temperature { get; set; } = Atmospherics.T20C;

    /// <summary>
    /// Thermal Conductivity in W/(K*m^2).
    /// Human skin is about 0.3 W/(m*K)
    /// Divide that by the thickness of skin of about 2mm giving us a final value of 150
    /// Source: https://pmc.ncbi.nlm.nih.gov/articles/PMC8953946/
    /// </summary>
    /// <remarks>
    /// This value should be multiplied by a surface area value based on the amount of area in contact.
    /// For Atmospherics, this is typically 2m^2, the surface area of the average body.
    /// </remarks>
    [DataField]
    public float ThermalConductivity = 150f;
}
