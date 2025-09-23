using Content.Shared.EntityEffects.Effects.StatusEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.EntitySpawning;

/// <summary>
/// Entity effect that specifically deals with new status effects.
/// </summary>
/// <typeparam name="T">The entity effect type, typically for status effects which need systems to pass arguments</typeparam>
public abstract partial class BaseSpawnEntityEntityEffect<T> : EntityEffectBase<T> where T : BaseSpawnEntityEntityEffect<T>
{
    /// <summary>
    /// Amount of entities we're spawning
    /// </summary>
    [DataField]
    public int Number = 1;

    /// <summary>
    /// Prototype of the entity we're spawning
    /// </summary>
    [DataField (required: true)]
    public EntProtoId Entity;
}
