using Content.Server.Body.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Body.Components;

[RegisterComponent]
[Access(typeof(ThermalRegulatorSystem))]
public sealed partial class ThermalRegulatorComponent : Component
{
    /// <summary>
    /// The next time that the body will regulate its heat.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdate;

    /// <summary>
    /// The interval at which thermal regulation is processed.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Heat generated due to metabolism. It's generated via metabolism
    /// </summary>
    [DataField("metabolismHeat")]
    public float MetabolismHeat { get; private set; }

    /// <summary>
    /// Heat output via radiation.
    /// </summary>
    [DataField("radiatedHeat")]
    public float RadiatedHeat { get; private set; }

    /// <summary>
    /// Maximum heat regulated via sweat
    /// </summary>
    [DataField("sweatHeatRegulation")]
    public float SweatHeatRegulation { get; private set; }

    /// <summary>
    /// Maximum heat regulated via shivering
    /// </summary>
    [DataField("shiveringHeatRegulation")]
    public float ShiveringHeatRegulation { get; private set; }

    /// <summary>
    /// Amount of heat regulation that represents thermal regulation processes not
    /// explicitly coded.
    /// </summary>
    [DataField("implicitHeatRegulation")]
    public float ImplicitHeatRegulation { get; private set; }

    /// <summary>
    /// Normal body temperature
    /// </summary>
    [DataField("normalBodyTemperature")]
    public float NormalBodyTemperature { get; private set; }

    /// <summary>
    /// Deviation from normal temperature for body to start thermal regulation
    /// </summary>
    [DataField("thermalRegulationTemperatureThreshold")]
    public float ThermalRegulationTemperatureThreshold { get; private set; }
}
