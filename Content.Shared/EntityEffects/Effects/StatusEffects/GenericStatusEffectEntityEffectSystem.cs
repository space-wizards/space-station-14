using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.StatusEffects;

/// <summary>
/// Applies a Generic Status Effect to this entity, which is a timed Component.
/// The amount of time the Component is applied is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
[Obsolete("Use ModifyStatusEffect instead")]
public sealed partial class GenericStatusEffectEntityEffectSystem : EntityEffectSystem<MetaDataComponent, GenericStatusEffect>
{
    [Dependency] private StatusEffectsSystem _status = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, GenericStatusEffect effect, EntityEffectData data)
    {
        var time = effect.Time * data.Scale;

        switch (effect.Type)
        {
            case StatusEffectMetabolismType.Update:
                if (effect.Component != String.Empty)
                    _status.TryAddStatusEffect(entity, effect.Key, time, true, effect.Component);
                break;
            case StatusEffectMetabolismType.Add:
                if (effect.Component != String.Empty)
                    _status.TryAddStatusEffect(entity, effect.Key, time, false, effect.Component);
                break;
            case StatusEffectMetabolismType.Remove:
                _status.TryRemoveTime(entity, effect.Key, time);
                break;
            case StatusEffectMetabolismType.Set:
                _status.TrySetTime(entity, effect.Key, time);
                break;
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class GenericStatusEffect : EntityEffect
{
    /// <summary>
    /// Identifier key for the status effect.
    /// </summary>
    [DataField(required: true)]
    public string Key = default!;

    /// <summary>
    /// Name of the component reflecting this status effect.
    /// </summary>
    [DataField]
    public string Component = String.Empty;

    /// <summary>
    /// Duraction of the effect.
    /// </summary>
    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(2f);

    /// <summary>
    ///     Should this effect add the status effect, remove time from it, or set its cooldown?
    /// </summary>
    [DataField]
    public StatusEffectMetabolismType Type = StatusEffectMetabolismType.Update;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => Loc.GetString(
        "entity-effect-guidebook-status-effect-old",
        ("chance", Probability),
        ("type", Type),
        ("time", Time.TotalSeconds),
        ("key", $"entity-effect-status-effect-{Key}"));
}
