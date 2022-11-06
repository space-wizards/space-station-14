using Robust.Shared.GameStates;

namespace Content.Shared.Weather;

[RegisterComponent, NetworkedComponent]
public sealed class WeatherComponent : Component
{
    /// <summary>
    /// Currently running weather.
    /// </summary>
    [ViewVariables, DataField("weather")]
    public string? Weather;

    /// <summary>
    /// When the applied weather will end.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("endTime")]
    public TimeSpan EndTime = TimeSpan.Zero;

    /// <summary>
    /// How long the weather will last in total from when first run.
    /// </summary>
    [ViewVariables]
    public TimeSpan Duration = TimeSpan.Zero;

    [ViewVariables]
    public WeatherState State = WeatherState.Starting;
}

public enum WeatherState : byte
{
    Starting,
    Running,
    Ending,
}
