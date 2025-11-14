using System.Numerics;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Weather;

/// <summary>
/// Uses only in conjure with <see cref="StatusEffectComponent"/> on map entities.
/// contains basic information about all types of weather effects
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedWeatherSystem))]
public sealed partial class WeatherStatusEffectComponent : Component
{
    [DataField(required: true)]
    public SpriteSpecifier Sprite = default!;

    [DataField]
    public Color? Color;

    /// <summary>
    /// Weather scrolling speed
    /// </summary>
    [DataField]
    public Vector2? Scrolling;

    /// <summary>
    /// Sound to play on the affected areas.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;

    // Client audio stream.
    [NonSerialized]
    public EntityUid? Stream;
}
