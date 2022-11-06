using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Weather;

[Prototype("weather")]
public sealed class WeatherPrototype : IPrototype
{
    [IdDataFieldAttribute] public string ID { get; } = default!;

    /// <summary>
    /// Minimum duration for the weather.
    /// </summary>
    public TimeSpan DurationMinimum = TimeSpan.FromSeconds(120);

    /// <summary>
    /// Maximum duration for the weather.
    /// </summary>
    public TimeSpan DurationMaximum = TimeSpan.FromSeconds(300);

    public TimeSpan StartupTime = TimeSpan.FromSeconds(30);

    public TimeSpan ShutdownTime = TimeSpan.FromSeconds(30);

    [ViewVariables(VVAccess.ReadWrite), DataField("sprite", required: true)]
    public SpriteSpecifier Sprite = default!;

    /// <summary>
    /// Sound to play on the affected areas.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("sound")]
    public SoundSpecifier? Sound;
}
