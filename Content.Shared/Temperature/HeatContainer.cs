using Content.Shared.Atmos;
using Content.Shared.Atmos.EntitySystems;
using Robust.Shared.Serialization;

namespace Content.Shared.Temperature;

/// <summary>
/// A general-purpose container for heat energy.
/// Any object that contains, stores, or transfers heat should use a <see cref="HeatContainer"/>
/// instead of implementing its own system.
/// This allows for consistent heat transfer mechanics across different objects and systems.
/// </summary>
[Serializable, NetSerializable, DataDefinition]
[Access(typeof(HeatContainerHelpers), typeof(SharedAtmosphereSystem))]
public partial record struct HeatContainer
{
    /// <summary>
    /// The heat capacity of this container.
    /// This determines how much energy is required to change the temperature of the container.
    /// Higher values mean the container can absorb or reject more heat energy
    /// without a significant change in temperature.
    /// </summary>
    [DataField]
    public float HeatCapacity = 4f;

    /// <summary>
    /// The current temperature of the container in Kelvin.
    /// </summary>
    [DataField]
    public float Temperature = Atmospherics.T20C;

    /// <summary>
    /// The current thermal energy of the container in Joules.
    /// </summary>
    [ViewVariables]
    public float ThermalEnergy => Temperature * HeatCapacity;
}
