using Content.Server.Body.Systems;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Body.Components;

[RegisterComponent]
[Friend(typeof(ThermalRegulatorSystem))]
public sealed class ThermalRegulatorComponent : Component
{
    /// <summary>
    /// Heat generated due to metabolism. It's generated via metabolism
    /// </summary>
    [ViewVariables]
    [DataField("metabolismHeat")]
    public float MetabolismHeat { get; private set; }

    /// <summary>
    /// Heat output via radiation.
    /// </summary>
    [ViewVariables]
    [DataField("radiatedHeat")]
    public float RadiatedHeat { get; private set; }

    /// <summary>
    /// Maximum heat regulated via sweat
    /// </summary>
    [ViewVariables]
    [DataField("sweatHeatRegulation")]
    public float SweatHeatRegulation { get; private set; }

    /// <summary>
    /// Maximum heat regulated via shivering
    /// </summary>
    [ViewVariables]
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
    [ViewVariables]
    [DataField("normalBodyTemperature")]
    public float NormalBodyTemperature { get; private set; }

    /// <summary>
    /// Deviation from normal temperature for body to start thermal regulation
    /// </summary>
    [DataField("thermalRegulationTemperatureThreshold")]
    public float ThermalRegulationTemperatureThreshold { get; private set; }

    public float AccumulatedFrametime;
}
