using Robust.Shared.Audio;
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

    // now
    public IPlayingAudioStream? Stream;

    /// <summary>
    /// When the weather started.
    /// </summary>
    [ViewVariables, DataField("startTime")]
    public TimeSpan StartTime = TimeSpan.Zero;

    /// <summary>
    /// When the applied weather will end.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("endTime")]
    public TimeSpan EndTime = TimeSpan.Zero;

    [ViewVariables]
    public TimeSpan Duration => EndTime - StartTime;

    [ViewVariables]
    public WeatherState State = WeatherState.Invalid;
}

public enum WeatherState : byte
{
    Invalid = 0,
    Starting,
    Running,
    Ending,
}
