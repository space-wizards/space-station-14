using Content.Shared.Atmos;
using Content.Shared.Temperature.Systems;

namespace Content.Shared.Temperature.Components;

/// <summary>
/// Handles tracking the temperature and heat capacity of an entity.
/// </summary>
[Access(typeof(SharedTemperatureSystem))]
[RegisterComponent]
public sealed partial class TemperatureComponent : Component
{
    /// <summary>
    /// Surface temperature which is modified by the environment.
    /// </summary>
    [DataField] // VV handled through SharedTemperatureSystem
    public float CurrentTemperature = Atmospherics.T20C;

    /// <summary>
    /// The amount of energy (in joules) required to increase the temperature of this entity by 1 kelvin.
    /// </summary>
    /// <remarks>
    /// Should not be directly accessed. Use <see cref="SharedTemperatureSystem.GetHeatCapacity"/>.
    /// </remarks>
    [Access(typeof(SharedTemperatureSystem), Other = AccessPermissions.None)] // VV handled through SharedTemperatureSystem
    public float CachedHeatCapacity = float.NaN;

    /// <summary>
    /// Whether the heat capacity needs to be recalculated 
    /// </summary>
    [Access(typeof(SharedTemperatureSystem), Other = AccessPermissions.None)]
    public bool HeatCapacityDirty = true;

    /// <summary>
    /// The base <see cref="CachedHeatCapacity"/>
    /// </summary>
    [DataField("heatCapacity")] // VV handled through SharedTemperatureSystem
    public float BaseHeatCapacity = 0f;

    /// <summary>
    /// Heat capacity per kg of mass.
    /// </summary>
    [DataField] // VV handled through SharedTemperatureSystem
    public float SpecificHeat = 50f;

    /// <summary>
    /// How well does the air surrounding you merge into your body temperature?
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float AtmosTemperatureTransferEfficiency = 0.1f;
}
