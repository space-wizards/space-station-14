using Content.Server.Atmos.EntitySystems;
using Content.Shared.Weather;
using Robust.Shared.Prototypes;

namespace Content.Server.Atmos.Components;

[RegisterComponent]
public sealed partial class WeatherDeviceComponent : Component
{
    [DataField]
    public bool Enabled;

    [DataField]
    public Dictionary<string, WeatherCycleState> KeyFramesByCycleState = new();

    [DataField]
    public TimeSpan EnabledChangeTime;

    [DataField]
    public string DefaultTickSpan;

    public TimeSpan DefaultTickSpanCasted { get; set; }

    [DataField]
    public WeatherStateMachine? StateMachine;

    public TimeSpan LastChanged = TimeSpan.Zero;

    
}

[Serializable, DataDefinition]
public partial record WeatherCycleState
{
    [DataField]
    public ProtoId<WeatherPrototype>? SetWeatherTo = null;

    [DataField]
    public bool ResetWeather = false;

    [DataField]
    public float? TargetTemperature = null;

    [DataField]
    public TimeSpan? WeatherOff;

    [DataField]
    public string? TickSpan;

    public TimeSpan? TickSpanCasted;

    /// <summary>
    /// Calculatable
    /// </summary>
    public float TickRate { get; set; }
}
