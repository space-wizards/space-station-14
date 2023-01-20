using Content.Shared.Maps;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.Utility;

namespace Content.Shared.Weather;

[Prototype("weather")]
public sealed class WeatherPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    /// <summary>
    /// Minimum duration for the weather.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DurationMinimum = TimeSpan.FromSeconds(120);

    /// <summary>
    /// Maximum duration for the weather.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DurationMaximum = TimeSpan.FromSeconds(300);

    [ViewVariables(VVAccess.ReadWrite), DataField("startupTime")]
    public TimeSpan StartupTime = TimeSpan.FromSeconds(30);

    [ViewVariables(VVAccess.ReadWrite), DataField("endTime")]
    public TimeSpan ShutdownTime = TimeSpan.FromSeconds(30);

    [ViewVariables(VVAccess.ReadWrite), DataField("sprite", required: true)]
    public SpriteSpecifier Sprite = default!;

    [ViewVariables(VVAccess.ReadWrite), DataField("color")]
    public Color? Color;

    /// <summary>
    /// Sound to play on the affected areas.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("sound")]
    public SoundSpecifier? Sound;
}
