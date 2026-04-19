using Content.Shared.Atmos;
using Content.Shared.Atmos.EntitySystems;
using Robust.Shared.Serialization;

namespace Content.Shared.Temperature.HeatContainer;

/// <summary>
/// A general-purpose container for heat energy.
/// </summary>
[Serializable, NetSerializable, DataDefinition]
[Access(typeof(HeatContainerHelpers), typeof(SharedAtmosphereSystem))]
public partial struct HeatContainer : IRobustCloneable<HeatContainer>, IHeatContainer
{
    /// <inheritdoc/>
    [DataField]
    public float HeatCapacity { get; set; } = 4000f; // about 1kg of water

    /// <inheritdoc/>
    [DataField]
    public float Temperature { get; set; } = Atmospherics.T20C; // room temperature

    /// <inheritdoc/>
    [ViewVariables]
    public float TemperatureC => TemperatureHelpers.KelvinToCelsius(Temperature);

    /// <inheritdoc/>
    [ViewVariables]
    public float InternalEnergy => Temperature * HeatCapacity;

    public HeatContainer(float heatCapacity, float temperature)
    {
        HeatCapacity = heatCapacity;
        Temperature = temperature;
    }

    /// <summary>
    /// Copy constructor for implementing ICloneable.
    /// </summary>
    /// <param name="c">The HeatContainer to copy.</param>
    private HeatContainer(HeatContainer c)
    {
        HeatCapacity = c.HeatCapacity;
        Temperature = c.Temperature;
    }

    public HeatContainer Clone()
    {
        return new HeatContainer(this);
    }
}
