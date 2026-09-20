namespace Content.Shared.Temperature.HeatContainer;

/// <summary>
/// Interface that defines a general-purpose container for heat energy.
/// Any object that contains, stores, or transfers heat should use a <see cref="HeatContainer"/>
/// or inherit <see cref="IHeatContainer"/> instead of implementing its own system.
/// This allows for consistent heat transfer mechanics across different objects and systems.
/// </summary>
public interface IHeatContainer
{
    /// <summary>
    /// The heat capacity of this container in Joules per Kelvin.
    /// This determines how much energy is required to change the temperature of the container.
    /// Higher values mean the container can absorb or release more heat energy
    /// without a significant change in temperature.
    /// </summary>
    float HeatCapacity { get; set; }

    /// <summary>
    /// The current temperature of the container in Kelvin.
    /// </summary>
    float Temperature { get; set; }

    /// <summary>
    /// The current temperature of the container in Celsius.
    /// Ideal if you just need to read the temperature for UI.
    /// </summary>
    float TemperatureC => TemperatureHelpers.KelvinToCelsius(Temperature);

    /// <summary>
    /// The current thermal energy of the container in Joules.
    /// </summary>
    float InternalEnergy => Temperature * HeatCapacity;
}

