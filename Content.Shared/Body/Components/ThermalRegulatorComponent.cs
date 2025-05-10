using Content.Shared.Body.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Body.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedThermalRegulatorSystem))]
public sealed partial class ThermalRegulatorComponent : Component
{
    /// <summary>
    /// The next time that the body will regulate its heat.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate;

    /// <summary>
    /// The interval at which thermal regulation is processed.
    /// </summary>
    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Heat generated due to metabolism. It's generated via metabolism
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MetabolismHeat;

    /// <summary>
    /// Heat output via radiation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RadiatedHeat;

    /// <summary>
    /// Maximum heat regulated via sweat
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SweatHeatRegulation;

    /// <summary>
    /// Maximum heat regulated via shivering
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ShiveringHeatRegulation;

    /// <summary>
    /// Amount of heat regulation that represents thermal regulation processes not
    /// explicitly coded.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ImplicitHeatRegulation;

    /// <summary>
    /// Normal body temperature
    /// </summary>
    [DataField, AutoNetworkedField]
    public float NormalBodyTemperature;

    /// <summary>
    /// Deviation from normal temperature for body to start thermal regulation
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ThermalRegulationTemperatureThreshold;
}
