using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;

namespace Content.Shared.Weather;

[RegisterComponent, NetworkedComponent]
public sealed partial class WeatherComponent : Component
{
    /// <summary>
    /// Currently running weathers
    /// </summary>
    [ViewVariables, DataField("weather", customTypeSerializer:typeof(PrototypeIdDictionarySerializer<WeatherData, WeatherPrototype>))]
    public Dictionary<string, WeatherData> Weather = new();

    public static readonly TimeSpan StartupTime = TimeSpan.FromSeconds(15);
    public static readonly TimeSpan ShutdownTime = TimeSpan.FromSeconds(15);
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class WeatherData
{
    // Client audio stream.
    [NonSerialized]
    public IPlayingAudioStream? Stream;

    /// <summary>
    /// When the weather started if relevant.
    /// </summary>
    [ViewVariables, DataField("startTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan StartTime = TimeSpan.Zero;

    /// <summary>
    /// When the applied weather will end.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("endTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? EndTime;

    [ViewVariables]
    public TimeSpan Duration => EndTime == null ? TimeSpan.MaxValue : EndTime.Value - StartTime;

    [DataField("state")]
    public WeatherState State = WeatherState.Invalid;

    [ViewVariables, NonSerialized]
    public float LastAlpha;

    [ViewVariables, NonSerialized]
    public float LastOcclusion;
}

public enum WeatherState : byte
{
    Invalid = 0,
    Starting,
    Running,
    Ending,
}
