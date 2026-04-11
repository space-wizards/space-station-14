using Content.Shared.EntityEffects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Weather.Effects;

/// <summary>
/// When added to a weather status effect entity (alongside <see cref="WeatherStatusEffectComponent"/>),
/// defines gameplay effects that are periodically applied to entities under open sky.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WeatherEntityEffectComponent : Component
{
    /// <summary>
    /// The entity effects to apply to exposed entities.
    /// </summary>
    [DataField(required: true)]
    public EntityEffect[] Effects = default!;

    /// <summary>
    /// Scale multiplier passed to EntityEffect.
    /// Modulates effect intensity (e.g. weather strength).
    /// </summary>
    [DataField]
    public float Scale = 1f;

    /// <summary>
    /// Optional: prototype ID of an <see cref="EntityEffectPrototype"/> to apply instead of inline Effects.
    /// If set, the Effects array is ignored.
    /// </summary>
    [DataField]
    public ProtoId<EntityEffectPrototype>? EffectPrototype;
}
