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
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<GenericStatusEffect> args)
    {
        var time = args.Effect.Time * args.Scale;

        switch (args.Effect.Type)
        {
            case StatusEffectMetabolismType.Update:
                if (args.Effect.Component != String.Empty)
                    _status.TryAddStatusEffect(entity, args.Effect.Key, time, true, args.Effect.Component);
                break;
            case StatusEffectMetabolismType.Add:
                if (args.Effect.Component != String.Empty)
                    _status.TryAddStatusEffect(entity, args.Effect.Key, time, false, args.Effect.Component);
                break;
            case StatusEffectMetabolismType.Remove:
                _status.TryRemoveTime(entity, args.Effect.Key, time);
                break;
            case StatusEffectMetabolismType.Set:
                _status.TrySetTime(entity, args.Effect.Key, time);
                break;
        }
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class GenericStatusEffect : EntityEffectBase<GenericStatusEffect>
{
    [DataField(required: true)]
    public string Key = default!;

    [DataField]
    public string Component = String.Empty;

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
