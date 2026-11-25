namespace Content.Shared.EntityEffects.Effects.StatusEffects;

/// <summary>
/// Entity effect that specifically deals with new status effects.
/// </summary>
/// <typeparam name="T">The entity effect type, typically for status effects which need systems to pass arguments</typeparam>
public abstract partial class BaseStatusEntityEffect<T> : EntityEffectBase<T> where T : BaseStatusEntityEffect<T>
{
    /// <summary>
    /// How long the modifier applies (in seconds).
    /// Is scaled by reagent amount if used with an EntityEffectReagentArgs.
    /// </summary>
    [DataField]
    public TimeSpan? Time = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Should this effect add the status effect, remove time from it, or set its cooldown?
    /// </summary>
    [DataField]
    public StatusEffectMetabolismType Type = StatusEffectMetabolismType.Update;

    /// <summary>
    /// Delay before the effect starts. If another effect is added with a shorter delay, it takes precedence.
    /// </summary>
    [DataField]
    public TimeSpan Delay;
}

public enum StatusEffectMetabolismType
{
    Update,
    Add,
    Remove,
    Set,
}
